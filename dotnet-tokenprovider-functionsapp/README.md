# Azure Functions token provider sample

>This code is provided without a service-level agreement, and it's not recommended for production workloads. Certain features might not be supported or might have constrained capabilities.

This project is a C# implementation of the code in [How to: Write a TokenProvider with an Azure Function](https://learn.microsoft.com/azure/azure-fluid-relay/how-tos/azure-function-token-provider). For detail information on how to write a token provider look at the document above.  

You can use [AzureFunctionTokenProvider](https://learn.microsoft.com/azure/azure-fluid-relay/how-tos/azure-function-token-provider#implement-the-tokenprovider) to test your code.

### Prerequisites
- .NET 6.0
- Azure Functions 4.0
- The following NuGet Packages (if not installed already):
  - Microsoft.IdentityModel.Tokens
  - System.IdentityModel.Tokens.Jwt