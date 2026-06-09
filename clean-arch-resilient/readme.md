[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/WvVNABOK)
#### NOTE
---
This is not the coursework handout. This is just a quick reference project readme.  
Please refer to the official handout from google classroom for more details.


#### Part 1
---
Taking the role of a backend engineer, you are requested to design and develop a system that will manage books.

A book entity must have a title, a publication date, a category, an author and the number of pages in it.

An author entity must have a first name, last name, country and number of books published.

Your system should fulfil all of the following requirements:

- Using the provided postgres (docker image) design your database tables that will store your books
- Support all CRUD (Create, Read, Update, Delete) operations
- Provide a search endpoint that will allow the API caller to search based on all book attributes (title, author, etc)
- Provide a bulk insert endpoint. This endpoint should support an operation for inserting and updating book details in big batches. First, it must expose a POST method that will allow the caller to provide a JSON array of items that should be inserted/updated. The caller should get a unique identifier for the job as a response indicating that the job is queued for processing. Posted items should be put into a buffer for parallel processing. These will be processed in batches of 10. (Find a reasonable size of parallel jobs that fits your hardware limitations)
- Provide an endpoint for checking the status of the bulk operation / job. Example of potential status of your job can be: Queued, In-progress, Completed, etc
- All endpoints provided should respect the RESTful principles with appropriate HTTP codes for relevant errors, etc

#### Don't
*Do not* use entity framework or any other ORM, use only raw sql  
*Do not* make views, there is no need for UI  


#### Part 2
---
Extremely pleased from your performance and the previous deliverables, the company is now requesting you to extend upon the previously submitted work.

During the project briefing, the company is sharing some security concerns with you. They would like you to enhance the previous endpoints with a token based authentication system and incoming throttling (20 Requests per 2 Seconds) in order to avoid any surprises.

Also, they are providing you with an external service that they would like to consume. This is an External REST API that provides IP address lookup and related services. Using this api (https://ipapi.co/), you are requested to protect the creation and deletion operations and restrict the origin of the requests to Greece. Any request coming for create and delete from any other country should be denied.

Other than that, you are requested to enhance all relevant services and/or repositories with in-app caching to optimise the usage of the persistent layer and external dependencies.

In order to keep up with the high quality of your previous work, you should focus on delivering fast, high quality code within specs. Proper HTTP responses/codes and good test coverage (above 80%)

To help you out with the requirement extraction, an in-house analyst is providing you with some user stories to include with your requirements.
