# proiect-licenta-rfid
Proiectarea și Implementarea unui Sistem  Bancar Simulat Bazat pe Tehnologie RFID și  Platformă Software de Interacțiune cu Utilizatorul
Pași pentru compilarea aplicației
**
Arduino (Nano ESP32)**

Deschide fișierul rfid_system.ino în Arduino IDE

Asigură-te că ai instalate următoarele biblioteci:

MFRC522

WiFi.h

RedisClient (sau ESP32_Redis)

AESLib + Crypto

Selectează placa: Arduino Nano ESP32

Selectează portul serial corect

Apasă Upload

**Aplicație C# (Interfață .NET)**

Deschide proiect.sln în Visual Studio

Verifică dacă ai instalat:

.NET Framework (minim versiunea dotnet 6)

StackExchange.Redis (via NuGet)

Build la proiect (Ctrl + Shift + B)

Rulează aplicația (F5)


**Pași de instalare**

Clonează repository-ul:

git clone https://github.com/mateitatar/proiect-licenta-rfid.git

Deschide directorul:

cd proiect-licenta-rfid

Deschide proiectul Arduino și cel C# în IDE-ul corespunzător

Configurează datele de conectare la WiFi și Redis în fișierele:

Arduino : ssid, password, redis_host

Formulare C# (toate care au nevoie de Redis) : redisHost, port, password

**Pași de lansare a aplicației**

Conectează Arduino Nano ESP32 la PC și pornește-l (cu cablu USB)

Pornește serverul Redis local sau în cloud

Rulează aplicația C#

Citește un card RFID – vor apărea opțiunile (activare, sold, etc.)

Simulează tranzacții în aplicație (bancomat sau magazin)



