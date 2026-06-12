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
| CI/CD | GitHub Actions |
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
   http://localhost:7118
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

# 🌐 Website

[Try it out!](https://app-pickemsplanter.azurewebsites.net/)

---

## 📸 Screenshots

<img width="1604" height="948" alt="Screenshot 2026-06-12 at 12 59 13" src="https://github.com/user-attachments/assets/8240d477-869e-4ce2-b5a4-fb019a8659d4" />
<img width="1604" height="948" alt="Screenshot 2026-06-12 at 12 59 44" src="https://github.com/user-attachments/assets/a872b9c0-2779-47d8-a9a6-9179a184151a" />
<img width="1604" height="948" alt="Screenshot 2026-06-12 at 13 00 18" src="https://github.com/user-attachments/assets/c31d3951-5c0f-4980-8bd8-bec6ce98407f" />
<img width="1604" height="948" alt="Screenshot 2026-06-12 at 13 00 40" src="https://github.com/user-attachments/assets/660533b9-0be9-4d5c-b809-e84c473b0f1a" />
