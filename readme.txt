The backend swagger site: https://planthomieapi20250519212023-g3dxbqerfvhhf0a6.northeurope-01.azurewebsites.net/swagger/index.html
The frontend: https://planthomie.z16.web.core.windows.net

To use the swagger, the user needs to login or signup as a user, which the person will then get an Bearer token that gives you autorization to the rest of the models tied to the specific user.

When you login or signup you will get an token where you then have to click the authorize button where you will then input "Bearer (yourtoken)" and then authorize. Now you will be able to POST and GET
APIs models from the swagger tied to the user.
