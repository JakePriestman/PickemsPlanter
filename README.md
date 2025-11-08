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
The goal is to recreate the experience of predicting match outcomes, tracking progress, and comparing result.

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

## 📸 Screenshots

<img width="3838" height="1917" alt="image" src="https://github.com/user-attachments/assets/900efced-de01-4706-ad57-7fda3135df5f" />
<img width="3839" height="1914" alt="image" src="https://github.com/user-attachments/assets/f57601d2-b38a-4ce0-9531-1dc73cd10795" />
<img width="3837" height="1916" alt="image" src="https://github.com/user-attachments/assets/9a410e18-ba09-43ef-8fe0-70208621d1f6" />
<img width="3839" height="1916" alt="image" src="https://github.com/user-attachments/assets/dce26a60-0873-4392-a07e-a32ade9d2c07" />
