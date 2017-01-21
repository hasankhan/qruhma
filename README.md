## Qabeelat Ruhma Analytics Platform

An application to track student registration for seminars

## Installation

* Create an [azure](azure.com) account 
* Create an [azure application](https://docs.microsoft.com/en-us/azure/app-service-web/app-service-web-how-to-create-a-web-app-in-an-ase)
* Configure [continuous deployment](https://docs.microsoft.com/en-us/azure/app-service-web/web-sites-deploy) and select 'External repository' option 
* Create a [documentdb](https://docs.microsoft.com/en-us/azure/documentdb/documentdb-create-collection) database with collection named 'registration'

## Configuration

* In your web app settings add the following settings
    * almaghrib_email: email address of person with admin access to almaghrib sites
    * almaghrib_password: password of the user above
    * slack_api_token: create an integration app in slack and put the api key (this is used to get volunteers list)
    * documentdb_key: the key to docdb database 
    * documentdb_baseurl: the url of your docdb instance

* Add [azure app authentication](https://docs.microsoft.com/en-us/azure/app-service-mobile/app-service-mobile-how-to-configure-active-directory-authentication) to protect your site using Facebook, Google or AAD  

## Questions?

Email me @ slashdot10 at gmail