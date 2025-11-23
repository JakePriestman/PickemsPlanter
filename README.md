# 🎯 PickemsPlanter

![GitHub last commit](https://img.shields.io/github/last-commit/JakePriestman/PickemsPlanter)
![GitHub license](https://img.shields.io/github/license/JakePriestman/PickemsPlanter)
![GitHub issues](https://img.shields.io/github/issues/JakePriestman/PickemsPlanter)
![GitHub pull requests](https://img.shields.io/github/issues-pr/JakePriestman/PickemsPlanter)
![Azure](https://img.shields.io/badge/Hosted%20on-Azure-blue?logo=microsoftazure)

> A .NET Razor Pages web app for managing your Counter-Strike 2 Pick’Ems — built for learning, experimentation, and fun.

---

## 🧠 Overview

**PickemsPlanter** is my personal project to build a **Razor Pages web app** that lets users create and manage their **Counter-Strike 2 Pick’Ems**.  
The goal is to recreate the experience of predicting match outcomes, tracking progress, and comparing results.

---

## 🧰 Tech Stack

| Layer | Technology |
|-------|-------------|
| Frontend | HTML, CSS, JavaScript |
| Backend | .NET Razor Pages |
| Cloud / Infra | Azure, Bicep, YAML (for pipelines & IaC) |
| CI/CD | GitHub Actions + Azure DevOps (YAML pipelines) |
| Hosting | Azure App Service |

---

## 🔗 Steam API Integration

This product fully complies with Valve's [API Terms of Use](https://steamcommunity.com/dev/apiterms).

- [Steam Web API](https://developer.valvesoftware.com/wiki/Steam_Web_API) - Used for management, setting and retrieving user Pick’Ems data as well as tournament data.

---

## ⚙️ Installation & Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- Git

### Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/JakePriestman/PickemsPlanter.git
   cd PickemsPlanter
   ```

2. **Restore .NET dependencies**
   ```bash
   dotnet restore
   ```

3. **Build and run locally**
   ```bash
   dotnet run
   ```

4. **Open in your browser**
   ```
   http://localhost:5000
   ```

---

## ☁️ Azure Deployment

You can deploy PickemsPlanter to **Azure App Service** using deploy.bicep

---

## 🧩 Features

- 🎮 Create and manage your CS2 Pick’Ems
- ☁️ Deployed and managed via Azure Bicep
- 🔄 CI/CD through YAML pipelines
- 🧱 Built entirely with .NET Razor Pages

---

## 📝 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## 🧑‍💻 Author

**Jake Priestman**  
- GitHub: [@JakePriestman](https://github.com/JakePriestman)  
- Project: [PickemsPlanter](https://github.com/JakePriestman/PickemsPlanter)

---

# 🌐 Website

[Try it out!](https://https://app-pickemsplanter.azurewebsites.net/)

---

## 📸 Screenshots

<img width="3838" height="1917" alt="image" src="https://github.com/user-attachments/assets/900efced-de01-4706-ad57-7fda3135df5f" />
<img width="3839" height="1914" alt="image" src="https://github.com/user-attachments/assets/f57601d2-b38a-4ce0-9531-1dc73cd10795" />
<img width="3839" height="1917" alt="image" src="https://github.com/user-attachments/assets/f8dff144-873f-4ff3-8095-6b9c973df2c9" />
<img width="3839" height="1921" alt="image" src="https://github.com/user-attachments/assets/61732013-8269-413b-ba4f-4c210f83a6c9" />


