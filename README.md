# C# Video Live Streaming

## About
This repository contains Visual Studios 2017 code that is written in C#. There are two solutions in there called ImageUDPServer and ImageUDPClient. 

The main features of the programs is to show how a UDP server can send raw image bytes to a UDP client which can then process the bytes to be used for displaying.

## ImageUDPClient
This solution contains two projects called ImageUDPClient and MultiImageUDPClient. 

ImageUDPClient connects to a server and just retrieves a single image from the server and saves it at a set location.

MultiImageUDPClient connects to a server and continously retrieves image bytes so it can then process and display in a PictureBox.

## ImageUDPServer
This solution contains only one project called ImageUDPServer.

ImageUDPServer first converts the images in the JPEG_Images\ folder into bytes and then waits for a client to connect. Once connection has been established, it will just continously send the raw bytes to the client to process.

## Prerequisites
* Visual Studios 2017

## Set up
* Download the zip.
* Extract the zip.
OR
* Clone the repository.

## Running
* Open two separate instanes of Visual Studios 2017.
* In one of them, open the ImageUDPServer solution and in the other open the ImageUDPClient solution.
* Build everything.
* Run an instance of the ImageUDPServer project.
* Run an instance of either the ImageUDPClient project.
