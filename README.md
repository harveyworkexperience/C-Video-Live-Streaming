# C# Video Live Streaming

## About
This repository contains Visual Studios 2017 code that is written in C#. There are two solutions in there called ImageUDPServer and ImageUDPClient. 

The main purpose of the programs is to learn how to code a UDP server to send raw image bytes to a UDP client which can then process the bytes to be used for displaying.



## ImageUDPClient
This solution contains two projects called ImageUDPClient and MultiImageUDPClient. 

ImageUDPClient connects to a server and just retrieves a single image from the server and saves it at a set location.

MultiImageUDPClient connects to a server and continously retrieves image bytes so it can then process and display in a PictureBox.



## ImageUDPServer
This solution contains only two projects called ImageUDPServer and MultiClientUDPServer.

ImageUDPServer first converts the images in the JPEG_Images\ folder into bytes and then waits for a client to connect. Once connection has been established, it will just continously send the raw bytes to the client to process.

MultiClientUDPServer demonstrates that it can allow multiple clients to subscribe to the service and receive the same streams. The outputting stream can be switched so that each client is receiving the same type of service.

***Note: The MultiClientUDPServer uses a lot of resources so instances of it should run on a computer that can handle it (Haven't tested thoroughly).***



## Prerequisites
* Visual Studios 2017



## Set up
* Download the zip.
* Extract the zip.

OR

* Clone the repository.



## Running on the same machine
1. Open two separate instanes of Visual Studios 2017.
2. In one of them, open the ImageUDPServer solution and in the other open the ImageUDPClient solution.
3. Build everything.
4. Run an instance of one of the two ImageUDPServer project.
5. Run an instance of one of the two ImageUDPClient project.
6. The client instance should be displaying a video in a PictureBox.



## Running on separate machines (Tested only through the same WiFi)
**Important:** Follow the **ImageUDPServer Machine** instructions ***Before*** following the **ImageUDPClient Machine** instructions. The reason for this is because the ImageUDPClient projects will just close if they don't instantly connect to a server (I might get around to fixing this later).

### ImageUDPServer Machine
1. Build and run an instance of one of the two ImageUDPServer project.
2. Copy the IPv4 address (Under the Wireless LAN Adapter WiFi).

### ImageUDPClient Machine
1. Open Windows Command Prompt.
2. Type in `ipconfig` and press enter.
3. Choose and open the ImageUDPClient or MultiImageUDPClient projects.
4. Replace the IP address in the IPEndPoint ep variable (Located near the top of the *Program.cs* file) to be the IP address you had just copied.
5. Save, build and run the instance.
6. The client instance should be displaying a video in a PictureBox.



## TODO/WISHLIST
* ~~UDPServer cannot serve multiple clients. So a new instance will need to run whenever, you want to try out a different client. Want to fix this.~~
* Add in a Real Time Transport library and integrate that with the solutions.
* Increase the speed of processing the bytes to be able to handle higher quality images.
* A client is unable to listen to the server while it is in the middle of broadcasting. Want to fix this.
