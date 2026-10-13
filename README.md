# AI-Powered Learning Management System (AI-LMS)

An AI-powered Learning Management System built using **ASP.NET Core Web API, C#, Entity Framework Core, SQL Server, and React**. The application helps manage courses, students, trainers, assessments, attendance, marks, certificates, and learning progress through a centralized platform.

## 🚀 Features

- **Authentication & Authorization** — User registration, login, role-based access, and JWT authentication.
- **Course Management** — Create and manage courses and lessons.
- **Student & Trainer Management** — Manage users, students, and trainers.
- **Assignments & Submissions** — Manage assignments and student submissions.
- **Quizzes & Assessments** — Support quizzes, questions, attempts, and scoring.
- **Attendance Management** — Track student attendance.
- **Marks & Progress Tracking** — Monitor student marks and learning progress.
- **Certificates** — Manage course completion certificates.
- **AI Learning Assistant** — AI-powered learning assistance.
- **Exception Handling & Validation** — Centralized API exception handling and request validation.
- **Unit Testing** — Automated tests using xUnit.

## 🛠️ Technologies Used

### Backend
- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- Swagger / OpenAPI
- xUnit

### Frontend
- React
- Vite
- JavaScript
- CSS

### Additional Technologies
- MongoDB integration for AI-related documents
- Git and GitHub
- .NET CLI and MSBuild

## 📁 Project Structure

```text
AI-LMS-DotNet/
├── backend/
│   ├── AI-LMS.API/
│   ├── AI-LMS.Application/
│   ├── AI-LMS.Domain/
│   ├── AI-LMS.Infrastructure/
│   └── AI-LMS.Tests/
├── frontend/
│   └── ai-lms-client/
├── tests/
│   └── AI-LMS.Tests/
├── AI-LMS.sln
├── Directory.Build.props
└── .gitignore
```

## ⚙️ Prerequisites

Install the following before running the application:

- .NET SDK compatible with the project
- Node.js and npm
- SQL Server
- Visual Studio or Visual Studio Code
- MongoDB, if using MongoDB-related functionality

## ▶️ Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Praveen2k02/AI-LMS-DotNet.git
cd AI-LMS-DotNet
```

### 2. Configure the backend

Configure the SQL Server connection string and other required settings using your local configuration or environment variables.

Do not commit real passwords, API keys, JWT secrets, or database credentials.

### 3. Run the backend API

```bash
dotnet restore
dotnet build
dotnet run --project backend/AI-LMS.API/AI-LMS.API.csproj
```

Open Swagger in your browser:

`http://localhost:5080/swagger`

If the API starts on a different port, use the URL shown in the terminal.

### 4. Run the frontend

Open another terminal:

```bash
cd frontend/ai-lms-client
npm install
npm run dev
```

Open the local URL printed by Vite, usually:

`http://localhost:5173`

### 5. Run unit tests

From the repository root:

```bash
dotnet test backend/AI-LMS.Tests/AI-LMS.Tests.csproj
```

## 🧪 Testing

The backend includes unit tests for authentication, course services, progress calculation, progress services, and quiz scoring.

Run the tests using the command above to verify the current implementation.

## 🔐 Security

- Keep production credentials outside source control.
- Use strong JWT signing secrets.
- Store passwords as secure hashes.
- Use role-based authorization to protect endpoints.
- Configure production secrets through environment variables or a secret manager.

## 🔮 Future Enhancements

- Deploy the application to a cloud platform.
- Add CI/CD automation using GitHub Actions.
- Improve AI-powered personalized learning recommendations.
- Add monitoring, logging, and production health checks.
- Expand automated testing and integration testing.

## 👨‍💻 Author

**Praveen**

GitHub: [Praveen2k02](https://github.com/Praveen2k02)

## 📄 License

This project is available for educational and portfolio purposes. Add a formal license file if you intend to distribute it under a specific open-source license.
