#include <Redis.h>
#include <ArduinoJson.h>
#include <SPI.h>
#include <MFRC522.h>
#include <WiFiUdp.h>
#include <NTPClient.h>
#include <TimeLib.h>  // Adăugăm TimeLib pentru a manipula datele
//#include <Crypto.h>
#include <AESLib.h>
//#include <Base64.h>

#define WIFI_SSID "Camera 302"
#define WIFI_PASSWORD "retardel43"

/*
#define REDIS_ADDR "redis-10874.c335.europe-west2-1.gce.redns.redis-cloud.com"
#define REDIS_PORT 10874
#define REDIS_PASSWORD "jURlxVjs4aBFrDtnHflQESldtNyXoAys"
*/

#define REDIS_ADDR "redis-16658.crce198.eu-central-1-3.ec2.redns.redis-cloud.com"
#define REDIS_PORT 16658
#define REDIS_PASSWORD "2kgm9DrslqbJ0DrbZHYyo4cKP83fmCss"

#define POST_FREQUENCY 5  // Frecvența în secunde

// Configurare RFID
#define SS_PIN 10  // SDA
#define RST_PIN 9  // RST
MFRC522 rfid(SS_PIN, RST_PIN);

#ifdef HAL_ESP32_HAL_H_  // ESP32
#include <WiFiClient.h>
#include <WiFi.h>
#else
#ifdef CORE_ESP8266_FEATURES_H  // ESP8266
#include <ESP8266WiFi.h>
#endif
#endif

WiFiClient redisConn;
Redis* gRedis = nullptr;

// NTP client pentru sincronizare cu ora, UTC+2 ca fus orar de bază (timpul României)
WiFiUDP ntpUDP;
NTPClient timeClient(ntpUDP, "pool.ntp.org", 2 * 3600, 60000);  // UTC+2, actualizare la fiecare 60 secunde

// Câmpuri Redis
String uidStr;
float balance = 0;
unsigned long scanCounter = 0;
String lastScanTime;
String lastScanDate;
String accessKey;  // Cheie de acces generată random
int isValid = 1;   // Camp nou - 1 pentru card activ, 0 pentru inactiv
//String command = "";  // Declarația variabilei 'command' care va primi comenzi din aplicația C#
bool isActivatingCard = false;  // Variabila pentru a urmări dacă procesul de activare a cardului este în curs

// Variabilă pentru controlul mesajului de așteptare
bool asteaptaCard = true;

// Cheie și IV pentru AES (16 bytes fiecare pentru AES-128)
AESLib aesLib;

// AES Key and IV (Initialization Vector) - Must be exactly 16 bytes each
#define INPUT_BUFFER_LIMIT (128 + 1)  // buffer limit for input text

unsigned char cleartext[INPUT_BUFFER_LIMIT] = { 0 };       // Input buffer (for text)
unsigned char ciphertext[2 * INPUT_BUFFER_LIMIT] = { 0 };  // Output buffer (for base64-encoded encrypted data)
unsigned char readBuffer[18] = "username:password";        // Example data to encrypt

// AES Encryption Key (same as in node-js example)
byte aes_key[] = { 0x2B, 0x7E, 0x15, 0x16, 0x28, 0xAE, 0xD2, 0xA6, 0xAB, 0xF7, 0x15, 0x88, 0x09, 0xCF, 0x4F, 0x3C };

// General initialization vector (same as in node-js example)
byte aes_iv[N_BLOCK] = { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

#define AES_KEY_SIZE 16
#define LED_PIN 3  // Pinul unde este conectat LED-ul


void setup() {
  Serial.begin(115200);
  SPI.begin();      // Initializează magistrala SPI
  rfid.PCD_Init();  // Initializează cititorul MFRC522
  aes_init();

  pinMode(LED_PIN, OUTPUT);    // Setăm pinul LED ca ieșire
  digitalWrite(LED_PIN, LOW);  // Asigurăm că LED-ul este stins inițial

  Serial.println();
  // Conectare la WiFi
  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  Serial.print("Conectare la WiFi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(250);
    Serial.print(".");
  }
  Serial.println();
  Serial.print("Adresa IP: ");
  Serial.println(WiFi.localIP());

  // Inițializare client NTP
  timeClient.begin();

  // Conectare la serverul Redis
  if (!redisConn.connect(REDIS_ADDR, REDIS_PORT)) {
    Serial.println("Conexiune eșuată la serverul Redis!");
    return;
  }

  gRedis = new Redis(redisConn);
  auto connRet = gRedis->authenticate(REDIS_PASSWORD);
  if (connRet == RedisSuccess) {
    Serial.printf("Conectat la serverul Redis la %s!\n", REDIS_ADDR);
  } else {
    Serial.printf("Autentificare eșuată la serverul Redis! Errno: %d\n", (int)connRet);
    return;
  }
}

void generateRandomAccessKey() {
  accessKey = "";
  for (int i = 0; i < 8; i++) {
    accessKey += String(random(0, 16), HEX);  // Cheie hexadecimală random cu 8 caractere
  }
  accessKey.toUpperCase();
}

void generateAESKey() {
  for (int i = 0; i < AES_KEY_SIZE; i++) {
    aes_key[i] = (uint8_t)esp_random(); // esp_random() returnează uint32_t, dar aici îl truncăm la byte
  }

  // Afișează cheia generată în HEX pentru verificare
  Serial.print("New AES Key: ");
  for (int i = 0; i < AES_KEY_SIZE; i++) {
    if (aes_key[i] < 0x10) Serial.print("0");
    Serial.print(aes_key[i], HEX);
  }
  Serial.println();
}

void updateTransactionHistory(int amount, const char* location, const char* product) {
  String transactionKey = "Transactii_" + uidStr;

  // Căutăm indexul următoarei tranzacții
  int tranzactiiCount = gRedis->llen(transactionKey.c_str());

  // Creăm un nume unic pentru fiecare tranzacție
  String transactionHashKey = transactionKey + "_" + String(tranzactiiCount);  // De exemplu: "Transactii_<UID>_0"

  // Criptăm suma
  String amountEncrypted = encryptAES(String(amount));

  // Creăm datele tranzacției (fără suma, care se salvează în hash)
  String transactionData = "{";
  transactionData += "\"location\":\"" + String(location) + "\",";
  transactionData += "\"product\":\"" + String(product) + "\",";
  transactionData += "\"time\":\"" + lastScanTime + "\"";
  transactionData += "}";

  // Salvăm tranzacția într-o listă
  gRedis->rpush(transactionKey.c_str(), transactionData.c_str());

  // Salvăm suma criptată într-un hash cu un nume unic
  gRedis->hset(transactionHashKey.c_str(), "amount", amountEncrypted.c_str());
}



void updateScanDate() {
  time_t epochTime = timeClient.getEpochTime();
  setTime(epochTime);  // Setează timpul local folosind timpul NTP

  int dayOfMonth = day();      // Corect: obține ziua din TimeLib
  int monthOfYear = month();   // Corect: obține luna din TimeLib
  int yearOfCentury = year();  // Corect: obține anul din TimeLib

  // Formatează data în stilul DD-MM-YYYY
  lastScanDate = String(dayOfMonth) + "-" + String(monthOfYear) + "-" + String(yearOfCentury);
}

// Function pentru criptarea unui string

void aes_init() {
  aesLib.gen_iv(aes_iv);
  aesLib.set_paddingmode((paddingMode)1);  // Set padding mode
}

uint16_t encrypt_to_ciphertext(char* msg, uint16_t msgLen, byte iv[]) {
  Serial.println("Calling encrypt (string)...");
  int cipherlength = aesLib.encrypt((byte*)msg, msgLen, (byte*)ciphertext, aes_key, sizeof(aes_key), iv);
  delay(50);
  return cipherlength;
}

uint16_t decrypt_to_cleartext(byte msg[], uint16_t msgLen, byte iv[]) {
  Serial.print("Calling decrypt...; ");
  uint16_t dec_bytes = aesLib.decrypt(msg, msgLen, (byte*)cleartext, aes_key, sizeof(aes_key), iv);
  delay(50);
  Serial.print("Decrypted bytes: ");
  Serial.println(dec_bytes);
  return dec_bytes;
}

void wait(unsigned long milliseconds) {
  unsigned long timeout = millis() + milliseconds;
  while (millis() < timeout) {
    yield();  // Allow other tasks to run
  }
}

unsigned long loopcount = 0;

// Working IV buffer
byte enc_iv[N_BLOCK] = { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

byte ivPin[16] = { 0 };                                                                                                // IV fix pentru PIN, poți să-l modifici la nevoie
byte keyPin[16] = { 0x2b, 0x7e, 0x15, 0x16, 0x28, 0x41, 0x61, 0x69, 0x70, 0x77, 0x7a, 0x79, 0x6b, 0x6f, 0x73, 0x75 };  // Cheia pentru PIN (trebuie să fie o cheie de 16 bytes)


String encryptAES(String data) {
    byte iv[16] = { 0 };  // IV fix pentru debug/testare
    uint16_t cipherLength = encrypt_to_ciphertext((char*)data.c_str(), data.length(), iv);
    
    if (cipherLength > 0) {
        //Serial.println("Mesaj criptat (hex):");
        //for (int i = 0; i < cipherLength; i++) {
            //Serial.print(ciphertext[i], HEX);
            //Serial.print(" ");
        //}
        Serial.println();
        
        // Crează un String din ciphertext pentru a-l returna
        String encrypted = "";
        for (int i = 0; i < cipherLength; i++) {
            encrypted += (char) ciphertext[i];
        }
        return encrypted;
    } else {
        Serial.println("Eroare la criptare!");
        return "";
    }
}



String decryptAES(String data) {
    byte iv[16] = { 0 };  // IV fix pentru testare
    
    // Conversie String -> byte array
    int len = data.length();
    byte encryptedBytes[len];
    for (int i = 0; i < len; i++) {
        encryptedBytes[i] = data[i];
    }

    uint16_t decLength = decrypt_to_cleartext(encryptedBytes, len, iv);

    if (decLength > 0) {
        //Serial.println("Mesaj decriptat (inclusiv padding):");
        //Serial.println((char*)cleartext);

        // Elimină padding-ul (caracterele '\x0C' sau altele)
        int actualLength = decLength;
        while (actualLength > 0 && cleartext[actualLength - 1] < 16) {
            actualLength--;
        }
        cleartext[actualLength] = '\0';  // Terminator pentru string

        //Serial.println("Mesaj curățat de padding:");
        //Serial.println((char*)cleartext);
        
        return String((char*)cleartext);  // Returnează textul curățat
    } else {
        Serial.println("Eroare la decriptare!");
        return "";
    }
}



String encryptPinAES(String pin) {
    byte iv[16] = { 0 };  // IV fix pentru PIN
    
    // Criptăm textul PIN
    uint16_t cipherLength = encrypt_to_ciphertext((char*)pin.c_str(), pin.length(), iv);

    if (cipherLength > 0) {
        // Convertim buffer-ul de ciphertext într-un string
        String encrypted = "";
        for (int i = 0; i < cipherLength; i++) {
            encrypted += (char)ciphertext[i];
        }
        return encrypted;  // Returnăm textul criptat
    } else {
        Serial.println("Eroare la criptarea PIN-ului!");
        return "";
    }
}

String decryptPinAES(String encryptedPin) {
    byte iv[16] = { 0 };  // IV fix pentru decriptare

    // Conversie String -> byte array
    int len = encryptedPin.length();
    byte encryptedBytes[len];
    for (int i = 0; i < len; i++) {
        encryptedBytes[i] = encryptedPin[i];
    }

    // Decriptăm textul criptat
    uint16_t decLength = decrypt_to_cleartext(encryptedBytes, len, iv);

    if (decLength > 0) {
        // Eliminăm padding-ul (caractere redundante de la sfârșit)
        int actualLength = decLength;
        while (actualLength > 0 && cleartext[actualLength - 1] < 16) {
            actualLength--;
        }
        cleartext[actualLength] = '\0';  // Terminator pentru string

        // Returnăm textul decriptat curățat de padding
        return String((char*)cleartext);
    } else {
        Serial.println("Eroare la decriptarea PIN-ului!");
        return "";
    }
}


String cleanDecryptedBalance(String decryptedBalance) {
  // Elimină orice caractere care nu sunt numere sau puncte
  decryptedBalance.replace("\r", "");  // Elimină carriage return
  decryptedBalance.replace("\n", "");  // Elimină newline
  decryptedBalance.trim();             // Elimină orice spațiu din față și spatele șirului

  return decryptedBalance;
}

void interogareSold(String uid) {
  // Obține soldul criptat din Redis folosind obiectul gRedis
  String encryptedBalance = gRedis->hget(uid.c_str(), "balance");

  // Verifică dacă am obținut un sold valid
  if (encryptedBalance != "") {
    // Decriptează soldul
    String decryptedBalance = decryptAES(encryptedBalance);  // Decriptarea efectivă
    //decryptedBalance = cleanDecryptedBalance(decryptedBalance);

    // Curăță de caractere invizibile
    String cleanBalance = cleanDecryptedBalance(decryptedBalance);

    // Afișează soldul curent
    Serial.print("Soldul curent al cardului cu UID: ");
    Serial.print(uid);
    Serial.print(" este ");
    //cleanBalance = cleanBalance.trim(); // Modifică cleanBalance direct
    Serial.println(cleanBalance);  // Afișează doar suma decriptată
  } else {
    Serial.println("Cardul nu are un sold stocat.");
  }
}



unsigned long lastInteractionTime = 0;    // Momentul ultimei interacțiuni
const unsigned long idleTimeout = 60000;  // Timeout de 60 secunde

void checkIdleTime() {
  unsigned long currentTime = millis();

  // Dacă timpul curent depășește timpul ultimei interacțiuni + timeout-ul
  if (currentTime - lastInteractionTime > idleTimeout) {
    Serial.println("IDLE timeout reached! No interaction detected.");
    lastInteractionTime = currentTime;  // Resetează pentru a evita mesaje repetate
    registerInteraction();
  }
}

void registerInteraction() {
  lastInteractionTime = millis();  // Resetează timpul de interacțiune
                                   //Serial.println("Interaction detected! Timer reset.");
}


void showSuccess() {
  digitalWrite(LED_PIN, HIGH);  // Aprinde LED-ul
  delay(1500);                  // Ține-l aprins 1.5 secunde
  digitalWrite(LED_PIN, LOW);   // Stinge LED-ul
}


String Cheie_Access;

void checkSerialCommands() {
  if (Serial.available()) {
    String command = Serial.readStringUntil('\n');
    command.trim();

    if (command.equalsIgnoreCase("Generate")) {
      generateAESKey();
    }
  }
}


void loop() {


  checkIdleTime();  // Verifică dacă a trecut timpul de inactivitate

  // Actualizare timp NTP
  timeClient.update();
  checkSerialCommands();
  String command = gRedis->get("command");  // Citește comanda din Redis

  String cosCumparaturiStr = gRedis->get("Pret_Cos_Cumparaturi");

  int currentMonth = timeClient.getEpochTime() / 2592000 % 12 + 1;
  if (currentMonth >= 3 && currentMonth <= 10) {
    timeClient.setTimeOffset(2 * 3600);  // UTC+2 pentru ora de vară
  } else {
    timeClient.setTimeOffset(2 * 3600);  // UTC+2 pentru ora de iarnă
  }

  if (asteaptaCard) {
    Serial.println("Se așteaptă urmatoarea comanda...");
    asteaptaCard = false;
  }

  if (!rfid.PICC_IsNewCardPresent() || !rfid.PICC_ReadCardSerial()) {
    delay(100);
    return;
  }

  asteaptaCard = true;
  uidStr = "";
  for (byte i = 0; i < rfid.uid.size; i++) {
    uidStr += String(rfid.uid.uidByte[i], HEX);
  }
  uidStr.toUpperCase();

  String storedCounter = gRedis->hget(uidStr.c_str(), "scan_counter");
  scanCounter = storedCounter != "" ? storedCounter.toInt() + 1 : 1;
  gRedis->hset(uidStr.c_str(), "scan_counter", String(scanCounter).c_str());

  if (storedCounter != "") {
    scanCounter = storedCounter.toInt() + 1;
  } else {
    scanCounter = 1;
  }



  lastScanTime = timeClient.getFormattedTime();
  updateScanDate();
  generateRandomAccessKey();

  String validStatus = gRedis->hget(uidStr.c_str(), "is_valid");
  if (command == "ACTIVARE_CARD") {
    // Verificăm dacă cardul este deja activat
    if (validStatus == "1") {
        Serial.printf("Cardul cu UID : %s este deja activat.\n", uidStr.c_str());
    } else {

        // Criptăm soldul (balance) (nu schimbăm logica balanței)
        String encryptedBalance = encryptAES(String(balance));

        // Preluăm numele complet și PIN-ul din Redis
        String numePrenume = gRedis->get("Nume_Prenume").c_str();
        String pin = gRedis->get("PIN_Card").c_str();

        // Verificăm dacă datele au fost primite corect
        if (numePrenume.length() == 0 || pin.length() == 0) {
            Serial.println("Eroare: Numele sau PIN-ul nu au fost trimise corect către Redis.");
            gRedis->set("command", ""); // Resetăm comanda
            return;
        }

        // Debug pentru PIN-ul primit din Redis
        Serial.printf("PIN primit din Redis pentru activare: '%s'\n", pin.c_str());

        // Criptăm PIN-ul
        String encryptedPin = encryptPinAES(pin);  // Folosim funcția dedicată pentru criptarea PIN-ului
      /*
        Serial.printf("Balance criptat: '%s'\n", encryptedBalance.c_str());
        Serial.printf("PIN criptat: '%s'\n", encryptedPin.c_str());
*/
        // Salvăm toate datele necesare în hash-ul cardului din Redis
        gRedis->hset(uidStr.c_str(), "UID", uidStr.c_str());
        gRedis->hset(uidStr.c_str(), "balance", encryptedBalance.c_str()); // Sold criptat
        gRedis->hset(uidStr.c_str(), "last_time_scanned", lastScanTime.c_str());
        gRedis->hset(uidStr.c_str(), "last_date_scanned", lastScanDate.c_str());
        gRedis->hset(uidStr.c_str(), "scan_counter", String(scanCounter).c_str());
        gRedis->hset(uidStr.c_str(), "access_key", accessKey.c_str());
        gRedis->hset(uidStr.c_str(), "is_valid", String(isValid).c_str());
        gRedis->hset(uidStr.c_str(), "nume_prenume", numePrenume.c_str());
        gRedis->hset(uidStr.c_str(), "pin", encryptedPin.c_str()); // PIN criptat

        // Actualizăm istoricul tranzacțiilor pentru activare
        updateTransactionHistory(0, "TOP UP", "Activation Fee");

        // Afișăm mesajul de succes
        Serial.printf("Cardul cu UID : %s a fost activat cu succes.\n", uidStr.c_str());

        // Resetăm comanda
        gRedis->set("command", "");

        // Afișăm mesaj de succes
        showSuccess();
    }
    registerInteraction();
}



  else if (command == "ANULARE_COMANDA") {
    Serial.println("Comanda a fost anulată.");
    isActivatingCard = false;
    asteaptaCard = false;
    gRedis->set("command", "");
    registerInteraction();
  } 
  
  else if (command == "ADAUGARE_NUMERAR") {
    // Verificăm dacă PIN-ul a fost validat
        // Verificăm dacă cheia de acces este validă și corespunde cu cheia cardului scanat
        String currentAccessKey = gRedis->hget(uidStr.c_str(), "access_key");

        if (Cheie_Access == currentAccessKey) {  // Cheia de acces este aceeași
            // Verificăm dacă cardul este activat (is_valid == "1")
            String validStatus = gRedis->hget(uidStr.c_str(), "is_valid");

            if (validStatus == "1") {  // Cardul este activ
                String sumaIntroducere = gRedis->get("suma_adaugare");
                if (sumaIntroducere != "") {
                    int suma = sumaIntroducere.toInt();

                    String soldCurentStr = gRedis->hget(uidStr.c_str(), "balance");
                    delay(500);
                    float soldCurent = decryptAES(soldCurentStr).toFloat();
                    delay(500);
                    //Serial.printf("Sold curent înainte de adăugare: %.2f\n", soldCurent);

                    if (suma <= 0) {
                        Serial.println("Suma introdusă nu poate fi 0.");
                    } else if (soldCurent + suma > 500) {
                        Serial.println("Suma totală depășește limita de 500 de lei.");
                    } else {
                        float soldNou = soldCurent + suma;
                        String encryptedNewBalance = encryptAES(String(soldNou));

                        int attempt = 0;
                        bool updated = false;
                        while (attempt < 10 && !updated) {
                            gRedis->hset(uidStr.c_str(), "balance", encryptedNewBalance.c_str());
                            delay(500);  // Așteaptă mai mult timp
                            String soldVerificat = gRedis->hget(uidStr.c_str(), "balance");
                            delay(500);
                            float soldActualizat = decryptAES(soldVerificat).toFloat();
                            delay(500);
                            if (soldActualizat == soldNou) {
                                updated = true;
                                Serial.println("Soldul a fost actualizat cu succes.");
                                showSuccess();
                            }
                            attempt++;
                            if (attempt == 10) {
                                Serial.println("Nu am reușit să actualizăm soldul după 10 încercări.");
                            }
                        }

                        if (updated) {
                            updateTransactionHistory(suma, "TOP UP", "Adăugare numerar");
                            Serial.printf("Suma a fost adăugată cu succes pentru UID: %s\n", uidStr.c_str());
                        } else {
                            Serial.println("Eroare la actualizarea soldului în Redis.");
                        }
                    }
                } else {
                    Serial.println("Nu s-a primit nici o sumă pentru adăugare.");
                }
            } else {  // Cardul nu este activ
                Serial.printf("Cardul cu UID: %s nu este activat. Adăugarea numerarului este blocată.\n", uidStr.c_str());
            }
        } else {  // Cheia de acces nu se potrivește
            Serial.println("Cheia de acces nu se potrivește cu cardul scanat. Adăugarea numerarului este blocată.");
        }
     

    // Resetează comanda după finalizarea procesului
    registerInteraction();
    gRedis->set("command", "");
    delay(500);  // Delay pentru a preveni executarea rapidă a mai multor comenzi
}



  else if (command == "INTEROGARE_SOLD") {
    // Verificăm dacă există un PIN validat
    String pinStatus = gRedis->get("pin_status");

    if (pinStatus == "VALID") {  // PIN-ul a fost validat
        // Verificăm dacă cheia de acces este validă și corespunde cu cheia cardului scanat
        String currentAccessKey = gRedis->hget(uidStr.c_str(), "access_key");

        if (Cheie_Access == currentAccessKey) {  // Cheia de acces este aceeași
            // Verifică dacă cardul este activat (is_valid == "1")
            String validStatus = gRedis->hget(uidStr.c_str(), "is_valid");

            if (validStatus == "1") {  // Cardul este activ
                // Obține soldul criptat din Redis
                String storedBalance = gRedis->hget(uidStr.c_str(), "balance");

                if (storedBalance != "") {
                    // Decriptează soldul
                    String decryptedBalanceStr = decryptAES(storedBalance);

                    // Curăță caracterele invizibile/nedorite
                    decryptedBalanceStr = cleanDecryptedBalance(decryptedBalanceStr);

                    if (decryptedBalanceStr != "") {
                        // Trimite soldul curent către Redis pentru afișare
                        gRedis->set("sold_value", decryptedBalanceStr.c_str());

                        Serial.print("Soldul curent al cardului cu UID: ");
                        Serial.print(uidStr);
                        Serial.print(" este ");
                        decryptedBalanceStr.trim();
                        Serial.println(decryptedBalanceStr);

                        // Afișare confirmare pe ecran (opțional)
                        showSuccess();
                    } else {
                        Serial.println("Eroare la decriptarea soldului / Soldul este gol.");
                    }
                } else {
                    Serial.println("Soldul cardului nu este stocat în Redis.");
                }
            } else {  // Cardul nu este activ
                Serial.printf("Cardul cu UID: %s nu este activat. Interogarea soldului a fost blocată.\n", uidStr.c_str());
            }
        } else {  // Cheia de acces nu corespunde
            Serial.println("Cheia de acces nu se potrivește cu cardul scanat. Interogarea soldului a fost blocată.");
        }

        // Resetăm statusul PIN-ului pentru următoarele operații
        gRedis->set("pin_status", "");
    } else {  // PIN-ul nu este validat
        Serial.println("PIN-ul nu este validat. Interogarea soldului nu poate continua.");
    }

    // Înregistrează interacțiunea și resetează comanda
    registerInteraction();
    gRedis->set("command", "");
    delay(500);  // Timp pentru procesare
}



  else if (command == "EXTRAGERE_NUMERAR") {
    // Verificăm dacă PIN-ul a fost validat
    

    
        // Verificăm dacă cheia de acces este validă și corespunde cu cheia cardului scanat
        String currentAccessKey = gRedis->hget(uidStr.c_str(), "access_key");
        delay(500);
        if (Cheie_Access == currentAccessKey) {  // Cheia de acces este aceeași
            // Verifică dacă cardul este activat (is_valid == "1")
            String validStatus = gRedis->hget(uidStr.c_str(), "is_valid");
            delay(500);
            if (validStatus == "1") {  // Cardul este activ
                // Citește suma de extras din Redis
                String sumaExtrasaStr = gRedis->get("suma_extragere");
                if (sumaExtrasaStr != "") {
                    int sumaExtrasa = sumaExtrasaStr.toInt();

                    // Citește soldul criptat din Redis
                    String soldCriptat = gRedis->hget(uidStr.c_str(), "balance");
                    delay(500);
                    String soldDecriptat = decryptAES(soldCriptat);
                    delay(500);
                    // Verifică dacă decriptarea a reușit
                    if (soldDecriptat == "") {
                        Serial.println("Eroare la decriptarea soldului curent.");
                        gRedis->set("command", "");  // Resetează comanda
                        return;
                    }

                    // Conversie la float
                    float soldCurent = soldDecriptat.toFloat();

                    // Verifică dacă soldul curent este valid
                    if (soldCurent <= 0) {
                        Serial.println("Sold invalid sau insuficient.");
                        gRedis->set("command", "");  // Resetează comanda
                        return;
                    }

                    // Verifică sumele introduse și disponibilitatea soldului
                    if (sumaExtrasa <= 0) {
                        Serial.println("Suma de extras trebuie să fie mai mare decât 0.");
                    } else if (sumaExtrasa > soldCurent) {
                        Serial.println("Fonduri insuficiente pentru această extragere.");
                    } else {
                        // Calculează soldul nou și criptează-l
                        float soldNou = soldCurent - sumaExtrasa;
                        String encryptedNewBalance = encryptAES(String(soldNou, 2));  // Precizie de 2 zecimale
                        delay(500);
                        // Loop de verificare până când suma în Redis este actualizată corect
                        int attempts = 0;
                        bool updated = false;
                        float soldActualizat = 0;

                        while (attempts < 10 && !updated) {
                            gRedis->hset(uidStr.c_str(), "balance", encryptedNewBalance.c_str());
                            delay(500);  // Așteaptă 500 ms pentru a permite actualizarea în Redis

                            // Verifică soldul după actualizare
                            String soldVerificat = gRedis->hget(uidStr.c_str(), "balance");
                            soldActualizat = decryptAES(soldVerificat).toFloat();
                            Serial.printf("Verificare (Încercarea %d)\n", attempts + 1);

                            if (soldActualizat == soldNou) {
                                updated = true;
                                Serial.println("Soldul a fost actualizat cu succes.");
                                showSuccess();
                            }
                            attempts++;
                            if (attempts == 10 && !updated) {
                                Serial.println("Eroare la actualizarea soldului în Redis după 10 încercări.");
                            }
                        }

                        if (updated) {
                            // Înregistrează extragerea în istoricul tranzacțiilor
                            updateTransactionHistory(-sumaExtrasa, "EXTRAGERE", "Retragere numerar");
                            Serial.printf("Suma de %d lei a fost extrasă cu succes pentru UID: %s\n", sumaExtrasa, uidStr.c_str());
                            showSuccess();
                        } else {
                            Serial.println("Nu am reușit să actualizăm soldul după 10 încercări.");
                        }
                    }
                } else {
                    Serial.println("Nu s-a primit nici o sumă pentru extragere.");
                }
            } else {  // Cardul nu este activ
                Serial.printf("Cardul cu UID: %s nu este activat. Extragerea numerarului este blocată.\n", uidStr.c_str());
            }
        } else {  // Cheia de acces nu se potrivește
            Serial.println("Cheia de acces nu se potrivește cu cardul scanat. Extragerea numerarului este blocată.");
        }
   

    // Resetează comanda
    registerInteraction();
    gRedis->set("command", "");
    delay(500);  // Delay pentru a preveni mai multe comenzi prea repede
}


  else if (command == "AFISARE_TRANZACTII") {
    int success = 0;

    // Verifică dacă cheia de acces este validă și corespunde cu cheia cardului scanat
    String currentAccessKey = gRedis->hget(uidStr.c_str(), "access_key");

    if (Cheie_Access == currentAccessKey) {  // Cheia de acces este aceeași
        // Verifică dacă cardul este activat (is_valid == "1")
        String validStatus = gRedis->hget(uidStr.c_str(), "is_valid");

        if (validStatus == "1") {  // Cardul este activ
            // Creează cheia pentru tranzacțiile cardului curent
            String tranzactiiKey = "Transactii_" + uidStr;

            // Obține numărul de tranzacții direct folosind `llen`
            int tranzactiiCount = gRedis->llen(tranzactiiKey.c_str());
            Serial.printf("Sunt %d tranzacții pe cardul cu UID: %s\n", tranzactiiCount, uidStr);

            // Dacă lista este goală, afișează mesajul corespunzător
            if (tranzactiiCount <= 0) {
                Serial.println("Nu există tranzacții pentru acest card.");
                gRedis->set("command", "");
                return;
            }

            // Parcurge tranzacțiile și le afișează
            for (int i = 0; i < tranzactiiCount; i++) {
                String tranzactie = gRedis->lindex(tranzactiiKey.c_str(), i);
                delay(250);
                if (tranzactie != "") {
                    Serial.printf("Tranzacția %d: %s\n", i + 1, tranzactie.c_str());

                    // Parcurgem și parsăm JSON-ul din tranzacție
                    StaticJsonDocument<1024> doc;
                    DeserializationError error = deserializeJson(doc, tranzactie);

                    if (error) {
                        Serial.print("Eroare la parsarea tranzacției: ");
                        Serial.println(error.c_str());
                        continue;
                    }

                    // Extragem datele
                    String locatie = doc["location"].as<String>();
                    String produs = doc["product"].as<String>();
                    String timp = doc["time"].as<String>();

                    // Obținem cheia pentru hash-ul tranzacției
                    String transactionHashKey = tranzactiiKey + "_" + String(i);  // De exemplu: "Transactii_<UID>_0"

                    // Preluăm suma criptată din hash
                    String sumaCriptata = gRedis->hget(transactionHashKey.c_str(), "amount");
                    delay(50);
                    // Decriptează suma și afișează tranzacția
                    String sumaDecriptata = "";
                    int attempts = 0;
                    while (sumaDecriptata == "" && attempts < 10) {
                        sumaDecriptata = decryptAES(sumaCriptata);
                        delay(100);
                        attempts++;
                        if (sumaDecriptata == "") {
                            Serial.println("Eroare la decriptarea sumei, încerc din nou...");
                            delay(500);  // Delay între încercări, pentru a da timp procesului
                        }
                    }

                    if (sumaDecriptata == "") {
                        Serial.println("Eroare la decriptarea sumei după 10 încercări.");
                        continue;  // Dacă nu reușește nici după 10 încercări, continuă la următoarea tranzacție
                    }

                    float suma = sumaDecriptata.toFloat();
                    success = 1;
                    Serial.printf("Locatie: %s, Suma: %.2f, Tipul tranzacției: %s, Ora: %s\n\n", locatie.c_str(), suma, produs.c_str(), timp.c_str());
                } else {
                    Serial.println("Tranzacția nu există sau a avut o eroare la citire.");
                }
            }
        } else {  // Cardul nu este activ
            Serial.printf("Cardul cu UID: %s nu este activat. Afișarea tranzacțiilor este blocată.\n", uidStr.c_str());
        }
    } else {  // Cheia de acces nu se potrivește
        Serial.println("Cheia de acces nu se potrivește cu cardul scanat. Afișarea tranzacțiilor este blocată.");
    }

    // Resetăm comanda
    registerInteraction();
    gRedis->set("command", "");

    // Afisare Bec verde
    if (success == 1) {
        showSuccess();
    }

    delay(500);
}






  else if (command == "DEZACTIVARE_CARD") {
    String validStatus = gRedis->hget(uidStr.c_str(), "is_valid");

    if (validStatus == "1") {
      gRedis->hset(uidStr.c_str(), "is_valid", "0");  // Dezactivează cardul
      Serial.printf("Cardul cu UID: %s a fost dezactivat cu succes.\n", uidStr.c_str());
      showSuccess();
    } else {
      Serial.printf("Cardul cu UID: %s este deja dezactivat sau nu există.\n", uidStr.c_str());
    }

    // Resetează comanda după finalizarea procesului de dezactivare
    registerInteraction();
    gRedis->set("command", "");
    delay(500);
  }

  else if (cosCumparaturiStr != "" && cosCumparaturiStr != "0") {
    // Convertim valoarea din coș în număr
    float pretCos = cosCumparaturiStr.toFloat();

    // Verificăm dacă cardul este activ
    String validStatus = gRedis->hget(uidStr.c_str(), "is_valid");

    if (validStatus == "1") {  // Cardul este activ
        // Citește soldul criptat din Redis
        String soldCriptat = gRedis->hget(uidStr.c_str(), "balance");
        String soldDecriptat = decryptAES(soldCriptat);

        // Verifică dacă decriptarea a reușit
        if (soldDecriptat == "") {
            Serial.println("Eroare la decriptarea soldului curent.");
            gRedis->set("command", "");  // Resetează comanda
            return;
        }

        // Conversie la float
        float soldCurent = soldDecriptat.toFloat();

        // Verifică dacă soldul curent este valid
        if (soldCurent <= 0) {
            Serial.println("Sold invalid sau insuficient.");
            //Adaugare cos cumparaturi pe null
            gRedis->set("Pret_Cos_Cumparaturi", "0");
            gRedis->set("command", "");  // Resetează comanda
            return;
        }

        // Verifică dacă coșul este mai mare decât soldul
        if (pretCos > soldCurent) {
            Serial.println("Fonduri insuficiente pentru achiziționarea produselor.");
            gRedis->set("Pret_Cos_Cumparaturi", "0");
            gRedis->set("command", "");  // Resetează comanda
            return;
        }

        // Dacă coșul este mai mic sau egal cu soldul, procesăm plata
        float soldNou = soldCurent - pretCos;
        String encryptedNewBalance = encryptAES(String(soldNou, 2));  // Precizie de 2 zecimale

        // Loop de verificare până când suma în Redis este actualizată corect
        int attempts = 0;
        bool updated = false;
        float soldActualizat = 0;

        while (attempts < 10 && !updated) {
            // Actualizează soldul în Redis
            gRedis->hset(uidStr.c_str(), "balance", encryptedNewBalance.c_str());
            delay(500);  // Așteaptă 500 ms pentru a permite actualizarea în Redis

            // Verifică soldul după actualizare
            String soldVerificat = gRedis->hget(uidStr.c_str(), "balance");
            String soldDecriptatVerificat = decryptAES(soldVerificat);
            soldActualizat = soldDecriptatVerificat.toFloat();

            // Debugging - afișează soldul după fiecare încercare
            //Serial.printf("Încercarea %d: Sold verificat = %.2f, Sold actualizat dorit = %.2f\n", attempts + 1, soldActualizat, soldNou);

            if (soldActualizat == soldNou) {
                updated = true;
                Serial.println("Soldul a fost actualizat cu succes.");
                showSuccess();
            }
            attempts++;

            // Verifică dacă am ajuns la maximul de încercări
            if (attempts == 10 && !updated) {
                Serial.println("Eroare la actualizarea soldului în Redis după 10 încercări.");
            }
        }

        if (updated) {
            // Înregistrează achiziția în istoricul tranzacțiilor
            updateTransactionHistory(-pretCos, "MAGAZIN", "Achiziție produs");
            Serial.printf("Achiziția de %.2f lei a fost realizată cu succes pentru UID: %s\n", pretCos, uidStr.c_str());

            String successData = "1|" + uidStr;  // 1 - pentru succes, urmat de UID
            gRedis->set("Cos_Cumparaturi_success", successData.c_str());

            showSuccess();
        } else {
            Serial.println("Nu am reușit să actualizăm soldul după 10 încercări.");
        }

        // Resetează valoarea coșului la 0
        gRedis->set("Pret_Cos_Cumparaturi", "0");

    } else {  // Cardul nu este activ
        Serial.printf("Cardul cu UID: %s nu este activat. Achiziția este blocată.\n", uidStr.c_str());
    }

    // Resetează coșul de cumpărături și comanda
    gRedis->set("Pret_Cos_Cumparaturi", "0");
    registerInteraction();
    gRedis->set("command", "");
    delay(500);  // Delay pentru a preveni mai multe comenzi prea repede
}
  
  
  
  else if (command == "CHECK_PIN") {

    validStatus = gRedis->hget(uidStr.c_str(), "is_valid");
    

    if (validStatus != "1") {  // Cardul nu este activ
        Serial.println("Cardul nu este activat.");
        //Cheie_Access = "";
        gRedis->set("pin_status", "INVALID");  // Setăm statusul PIN-ului la "INVALID"
        gRedis->set("command", "");  // Resetăm comanda
        return;  // Ieșim din funcție, nu mai verificăm PIN-ul
    }

    // Obținem PIN-ul introdus de utilizator din Redis
    String pinIntrodus = gRedis->get("pin_introdus");

    // Obținem PIN-ul criptat din hash-ul cardului folosind UID-ul și îl decriptăm
    String pinSalvat = decryptPinAES(gRedis->hget(uidStr.c_str(), "pin"));  // "pin" este câmpul din hash-ul cardului
    delay(50);
    // Eliminăm orice caractere de padding sau control, de exemplu, '\f'
    pinSalvat.trim();  // Această metodă va elimina spațiile și caracterele de control la începutul și sfârșitul string-ului

    // Debug pentru valori
    //Serial.printf("PIN introdus din Redis: '%s'\n", pinIntrodus.c_str());
    //Serial.printf("PIN salvat decriptat: '%s'\n", pinSalvat.c_str());

    // Debug pentru lungimea string-urilor
    //Serial.printf("Lungime PIN introdus: %d\n", pinIntrodus.length());
    //Serial.printf("Lungime PIN salvat: %d\n", pinSalvat.length());

    // Verificăm dacă există diferențe de formatare între PIN-ul introdus și cel salvat
    if (pinIntrodus.length() != pinSalvat.length()) {
      Serial.println("Diferență de lungime între PIN-ul introdus și cel salvat.");
    }

    // Debug pentru comparație detaliată caracter cu caracter
    for (int i = 0; i < max(pinIntrodus.length(), pinSalvat.length()); i++) {
      char pinIntrodusChar = (i < pinIntrodus.length()) ? pinIntrodus[i] : '\0';  // Evităm depășirea limitelor
      char pinSalvatChar = (i < pinSalvat.length()) ? pinSalvat[i] : '\0';        // Evităm depășirea limitelor
      Serial.printf("Char %d: introdus='%c' (%d), salvat='%c' (%d)\n", i, pinIntrodusChar, pinIntrodusChar, pinSalvatChar, pinSalvatChar);
    }

    // Comparăm PIN-urile
    if (pinIntrodus == pinSalvat) {
      Serial.println("PIN-ul este corect.");

      Cheie_Access = gRedis->hget(uidStr.c_str(), "access_key");

      gRedis->set("pin_status", "VALID");  // Setăm statusul PIN-ului la "VALID"
    } else {
      Serial.println("PIN-ul este greșit.");
      gRedis->set("pin_status", "INVALID");  // Setăm statusul PIN-ului la "INVALID"
      Cheie_Access = "";
    }

    // Resetăm comanda
    
    gRedis->set("command", "");
}



  rfid.PICC_HaltA();
  delay(POST_FREQUENCY * 1000);
}
