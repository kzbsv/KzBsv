[KzBsv](https://github.com/kzbsv/KzBsv)
=

This repository contains the following .NET C#, Visual Studio 2019 Preview compatible projects:

* [KzBsv](https://github.com/kzbsv/KzBsv/tree/master/KzBsv): Bitcoin SV library for .NET 5.0

* [bricks](https://github.com/kzbsv/KzBsv/tree/master/bricks): Supporting projects.

  * [KzSecp256k1.Net](https://github.com/kzbsv/KzBsv/tree/master/bricks/KzSecp256k1.Net): A customized fork for access to high performance Secp256k1 C++ library.

* [tests](https://github.com/kzbsv/KzBsv/tree/master/tests): Unit Tests.
 
  *  [Tests.KzBsv](https://github.com/kzbsv/KzBsv/tree/master/tests/Tests.KzBsv): Unit tests for the KzBsv library.
  *  [Tests.KzSecp257k1.Net](https://github.com/kzbsv/KzBsv/tree/master/tests/Tests.KzSecp256k1.Net): Unit tests for the KzSecp257k1.Net library.

Changelog
-

**v0.2.0**
* Current version.

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

Why .Net 5.0?
-

.Net 5.0 is the path forward for new work using the .NET platform.

KzBsv Implemementation Status
-

Version 0.2.0 is a work in process update. For compatibility with other projects released in 2019 use version 0.1.6.

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


