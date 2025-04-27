# Gungnir

A Student Information System (version 2.0) developed using Windows Presentation Foundation (WPF), designed to 
efficiently manage college records, program information, and student data. MySQL has been integrated as its
primary database.


## Dashboard Demo
![dashboard_demo](https://github.com/user-attachments/assets/aad25080-9f0d-4ad8-b557-36723d06d063)


## Notes and Remarks

This project is a work in progress. The current version is a prototype and is intended for demonstration purposes only.


To access the application, register a new account by clicking the "Register" button.

If you are encountering an issue where it says "Run a NuGet package restore to generate this file", please follow the steps below:
- (Based in Visual Studio) From Tools > NuGet Package Manager > Package Manager Console simply run:

```
dotnet restore
```

The error occurs because the dotnet command line interface does not create all of the required files initially. Doing "dotnet restore" adds the required files.

## Features

- **User Authentication**
- **College Management**
- **Program Management**
- **Student Management**

## Installation

1. Clone the repository

```
  git clone https://github.com/keaneph/Gungnir
```

2. Open the project (*sis-app.sln*) in Visual Studio
3. Build the project
4. Run the project


## Roadmap

- Migration as a dynamic web application (ASP.NET Core)
- Feature enhancements like history tracking
- User roles and permissions
- Improved data validation
- Data export, import, backup, and restore


## Contributing

Contributions are always welcome!

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on my code of conduct, and the
process for submitting pull requests to me.


## Built With
- C# (.NET Framework)
- Windows Presentation Foundation (WPF)
- XAML
- MySQL


## Author
* [@keaneph](https://github.com/keaneph) - *Initial work*


## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) for details


## Acknowledgements

* [@SamHuertas](https://github.com/SamHuertas) - for his biased criticism and meaningless feedback
* [@brexer](https://github.com/brexer) - for lending his mobile hotspot during the development phase
* [@Ordinary33](https://github.com/Ordinary33) - for his wavering support and discouragement
