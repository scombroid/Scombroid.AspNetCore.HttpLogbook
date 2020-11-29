# Scombroid.AspNetCore.HttpLogbook
A library to provide logging for ASP.NET Core http request and response

# Get Started
Install the latest version from NuGet
```
Install-Package Scombroid.AspNetCore.HttpLogbook
```

# Usage
Add the following code to the ConfigureServices(...)
```
	services
		.AddHttpLogbook(Configuration.GetSection("HttpLogbook"))
		.AddLogger<MyHttpLogbookLogger>();
```    
Add the following code to the Configure(...)
```
  app.UseLogbook();
```

# Example Configuration
```
"HttpLogbook": {
    "Paths": {
      "*": {
        "Methods": {
          "*": {
            "Request": {
              "LogLevel": "Information"
            },
            "Response": {
              "LogLevel": "Information"
            }
          }
        }
      },      
      "/weatherforecast": {
        "Methods": {
          "GET": {
            "Request": {
              "LogLevel": "Information"
            },
            "Response": {
              "LogLevel": "None"
            }
          },
          "POST": {
            "Request": {
              "LogLevel": "Trace"
            },
            "Response": {
              "LogLevel": "Information"
            }
          }
        }
      }
    }
  }
```
