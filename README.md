# SETUP GUIDE

## Prerequisites
* .NET 8.0 SDK
* PostgreSQL 18
* Python 3.x (Optional, for utility scripts)
* MQTT Broker (Mosquitto) - Optional

---

## 1. Database Configuration
The project requires a PostgreSQL relational database to store sensor data and system settings.

**1.1 Locate SQL File**
The schema and initial data are located at: `/Database/irrigation_db.sql`

**1.2 Create Database**
Execute the following command in your terminal to create the database:
```powershell
psql -U postgres -c "CREATE DATABASE irrigation_db;"
```

**1.3 Import the Schema**
Populate the database using the following command:
```powershell
psql -U postgres -d irrigation_db -f Database/irrigation_db.sql
```

---

## 2. Application Configuration
Link the application to your local database instance by updating the configuration file.

**2.1 Navigate to Configuration**
Open the file located at: `IrrigationSystem.Web/appsettings.json`

**2.2 Update Connection Strings**
Update the `ConnectionStrings` section with your local credentials:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=irrigation_db;Username=postgres;Password=YOUR_PASSWORD"
}
```

---

## 3. Execution
Follow these steps to build and start the web server.

**3.1 Restore Dependencies**
```powershell
dotnet restore
```

**3.2 Start Web Server**
```powershell
dotnet run --project IrrigationSystem.Web
```

**3.3 Access Dashboard**
Open a web browser and navigate to: `http://localhost:5000` or `http://your-ipv4:5000`

---

## 4. Unit Testing
To verify the integrity of the service layer and API controllers, execute the xUnit test suite:
```powershell
dotnet test
```

---

## 5. Password Management Utility (Optional)
The repository includes a Python script (`hash_passwords.py`) used to generate secure hashes for manual database insertions. 

**Security Note**: This script utilizes `bcrypt`. Because bcrypt uses salting, it generates a unique hash every time, even for identical passwords. This provides strong protection against wordlist and brute-force attacks, demonstrating security best practices within the system architecture.

**5.1 Install Requirements**
```powershell
pip install bcrypt
```

**5.2 Edit the hash_passwords.py**
```powershell
import bcrypt



## Скрипт за хеширане на пароли. Може да се добавят нови пароли също така. Важно е да се отбележи, че bcrypt генерира различен хеш всеки път, дори за една и съща парола, поради използването на salting. Това прави хешовете по-сигурни срещу атаки с wordlist и bruteforcing. Не е нещо много важно за поливна система, но е интересно да се има :D



youruser_password = "yourpassword"



youruser_hash = bcrypt.hashpw(youruser_password.encode(), bcrypt.gensalt()).decode()



print(f"youruser hash: {youruser_hash}")
```


**5.3 Run the Script**
```powershell
python hash_passwords.py
```
Copy the generated hash outputs and insert them directly into the `users` table in your PostgreSQL database.

---

## 6. Technical Notes
* **MQTT Connectivity**: The system features graceful degradation. If an MQTT broker is not detected, the application remains fully functional via HTTP/REST communication.
* **Architecture**: The application is built using a three-tier architecture (Data, Service, and Presentation layers) to ensure modularity and scalability.

## Acknowledgments and References
The following resources and tools were utilized during development:

- **Frameworks & Libraries:** ASP.NET Core 8.0, Entity Framework Core, Npgsql, MQTTnet, and Moq (xUnit).
- **Security:** The `bcrypt` Python library was used for password hashing demonstrations.
- **AI Collaboration:** Gemini (Google AI) was used as a technical collaborator for code optimization, architectural advice, and documentation assistance.
- **Documentation:** Official Microsoft .NET and PostgreSQL documentation.
