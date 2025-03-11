# PDQ Hash

This is a pure .NET implementation of the PDQ hashing alorithm originally developed by [Meta](https://github.com/facebook/ThreatExchange/tree/main/pdq). 

## Who are we?

Resolver, (part of Kroll inc.), previously Crisp Thinking UK Ltd are a major provider of Trust and Safety services across the globe for consumers of, and providers of online digital services. 

## What motivates us

We have decided to open source this library with the hope that this library will make it easier for other digital service platforms that use the .NET stack to integrate Trust and Safety tools into their development stacks.

Our guiding priciples are:
 - Keep the library fast.
 - Provide easy to use CLI tools to generate and compare hashes. 
 - Ensure compliance with the python implementation of the PDQ hashing algorithm with automation, by following metas principals that we will not generate hashes with a greater distance of 11 from the reference implementation.

 ## What this library is not

 - This library will not focus on integrating with any particular hash providers.
 - This library will remain agnostic to platforms to ensure the widest compatibility with as many solutions as possible.

### Shoutouts

The interpretation of images relies on the fantastic work by the at [SkiaSharp](https://github.com/mono/SkiaSharp), and the video processing relies on the great contributions provided by [FFMpeg.Core](https://github.com/rosenbjerg/FFMpegCore).

### Contributors

<!-- readme: abbottdev,contributors -start -->
<!-- readme: contributors -end -->




