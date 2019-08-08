KzBsv
=

[![NuGet](https://img.shields.io/nuget/v/KzBsv.svg)](https://www.nuget.org/packages/KzBsv)
[![NuGet](https://img.shields.io/telegram/v/KzBsv.svg)](https://t.me/KzBsv)

A C# Bitcoin SV library for .NET Core 3.0

Tests for this library are found here:

* [Tests.KzBsv](https://github.com/kzbsv/KzBsv/tree/master/tests/Tests.KzBsv): Unit tests for the KzBsv library.

This project depends on the this project:

* [KzSecp256k1.Net](https://github.com/kzbsv/KzBsv/tree/master/bricks/KzSecp256k1.Net): A customized fork for access to high performance Secp256k1 C++ library.


KzBsv Implemementation Status
-

Implementation focus has been to notice and avoid issues for large script values,
large transactions and very large blocks. One way this is achieved by using chained and reused buffers instead
of requiring that objects always fit in contiguous bytes and that multiple copies can easily be made.

Unlike the C++ code, there's no support for automatic clearing of memory containing private keys.

Available Functionality:

  * HD Key support.
  * Serializing transactions is partially done.
  * Transaction signing.
  * RPC call to submit transactions to the network.
  * Initial JSON-RPC and ZMQ support. Patterns to follow for extending.
  * Main-Net.
  * Paymail client.

Paritally Completed Functionality:
  * Script interpreter is done except for the last big hill: MULTISIG and the Core crap.
  * Transaction and script builder.

Planned Functionality:
  * Network support: Connections to peers. Implementation of peer-to-peer protocols.
  * Triaged UTXO management.
  * Reach and maintain functional parity with Javascript Bsv library.
  * Metanet protocol support.
  * Support Testnet and Stn.
  * Support containerized deployments.

Why C#?
-

The combination of C# and Visual Studio is a productivity sweet spot.
Sure there are safer languages, higher performance languages, more widely portable languages, 
easier to get started languages. But in each case, the margin is thin and in many cases C# has a solid advantage.

Performance is important to Bitcoin projects.
Especially plumbing level projects of which there are many in desperate need of doing.
Scale and performance normally argue for un-managed, un-garbage collected languages.
But with the addition of support for Span\<T>, Memory\<T>, and ReadOnlySequence\<T>, this advantage is shrinking.
See this [blog post by Johannes Vermorel](https://blog.vermorel.com/journal/2019/1/8/salient-bits-of-cashdb.html) for more information. 

Why .Net Core 3.0 vs .Net Framework?
-

Unless you're in this space it probably seems silly, but just as Bitcoin has forked, so has the .NET platform. In this case though its a very
healthy and robustly supported fork by the greater development community and strongly supported by Microsoft.

.Net Core has a primary objective of being platform agnostic. There are development and deployment options for most operating systems and most devices.
Almost all of the libraries are open source.

.Net Framework is valuable for legacy support which is not relevant here. It dates from Microsoft's former strategy of tight control of their source code.
