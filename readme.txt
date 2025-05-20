The backend swagger site: https://planthomieapi20250519212023-g3dxbqerfvhhf0a6.northeurope-01.azurewebsites.net/swagger/index.html
The frontend: https://planthomie.z16.web.core.windows.net

How to Use Swagger Authentication.
To access the Swagger APIs, you must first log in or sign up. After doing so, you’ll receive a Bearer token. This token authorizes access to API models tied to your user account.

Authorize in Swagger
Once you have your token:

Click the "Authorize" button in Swagger.

Enter your token in the format:
Bearer your_token_here

Click Authorize to confirm.

You’ll now be able to use the POST and GET endpoints tied to your user.

Performance Note
The backend (including SQL) is hosted on Azure with limited vCores. As a result, it may take some time to start up. This can cause delayed responses from the frontend (timeouts may occur after ~20 seconds).
