# XML Signer with SoftHSM2

## Installation :

### Clone Repository :
    git clone https://github.com/yasinciftcii/xmlSigner.git
    cd xmlSigner
    dotnet build
    dotnet run

### SoftHSM2 Library Integration :
1. Download [SoftHSM2 Library](https://github.com/disig/SoftHSM2-for-Windows "SoftHSM2 Library") here.
2. Add the directory C:\SoftHSM2\bin to the PATH environment variable.
3. Before using SoftHSM2 you need to create the configuration file:
	- softhsm2.conf in a suitable directory (e.g. C:\SoftHSM2\etc\softhsm2.conf).
	- Inside the file, set the directories.tokendir parameter as follows:
	`directories.tokendir = C:\SoftHSM2\tokens`
4. Create a new token:
	`softhsm2-util --init-token --slot 0 --label "MyToken"`
	The command will prompt you to set the SO PIN (Security Officer PIN) and user PIN.
	
	You can use the following command to view your Slot and Token information:
	`softhsm2-util --show-slots`
5.  Add the path to your SoftHSM2 library where “YourPath” is written in the code.
6. Enter the your PIN code you created with your SoftHSM2 library where “yourPassword” is written in the code.

### Generate Private Key & Public Key & Certificates with OpenSSL
1. Generate a Private Key
	`openssl genpkey -algorithm RSA -out privateKey.pem -aes256`
2. Generate a Public Key
	`openssl rsa -in privateKey.pem -pubout -out publicKey.pem`
3. Generate a Certificate Request File (CSR)
	`openssl req -new -key privateKey.pem -out cert.csr`
4. Generate a Certificate
	`openssl x509 -req -days 365 -in cert.csr -signkey privateKey.pem -out cert.pem`
