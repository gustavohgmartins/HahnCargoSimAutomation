# The challenge

You are working for a transport company. The company offers you access to their map. This map is composed of **nodes**, representing where pickup and delivery locations are. It has also **edge’s** that describe how long travel time is and how much it costs to use them. And finally, **connections** that tell you what edge connects which two nodes. 
You start with 1000 coins and can also buy your first cargo transporter for free. Additional transporter cost 1000 coins. 
Orders will pop up over time and you can accept them. But be aware, if you fail to deliver them, it will cost you! 
Your job is to automate your cargo transporter – and to make more coins! 


# The solution

Docker Compose was employed to orchestrate the deployment of a .NET 8 backend and an Angular 16 frontend. For asynchronous communication between the APIs, RabbitMQ was utilized for receiving orders. Additionally, SignalR was integrated to enable real-time logging of automation actions performed by the backend to the frontend.

The maximum number of transporters can be configured within the appsettings.json file (it depends on how long the application will be running for)

  Default value: 5

  Set it to 0 to be unlimited (as soon as possible, according to the implemented rule, a new transporter will be bought)

## Getting started

Make sure to have RabbitMQ and Docker installed.

1- Start RabbitMQ, and log in.

  Default port: http://localhost:15672

  Default user: guest

  Default password: guest

2- Start Docker Engine

3- Start HahnCargoSim project

4- At the docker-compose.yml directory run the following command:
docker compose up

5- Access the application at http://localhost:80


