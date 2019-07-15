KzBsv
=

A C# Bitcoin SV library for .NET Core 3.0

Tests for this library are found here:

* [Tests.KzBsv](https://github.com/tonesnotes/KzBitcoinSV/tree/master/Tests.KzBsv): Unit tests for the KzBsv library.

A rapid prototyping demonstration application can be found here:

* [KzHack](https://github.com/tonesnotes/KzBitcoinSV/tree/master/KzHack): A demonstration application developed for the [2019 Bitcoin Association Hackathon](https://bitcoinassociation.net/hackathon/)

This project depends on the this project:

* [bricks](https://github.com/tonesnotes/KzBitcoinSV/tree/master/bricks): Supporting projects including:
  * [Secp256k1.Net](https://github.com/MeadowSuite/Secp256k1.Net): A customized fork for access to high performance Secp256k1 C++ library.


KzBsv Implemementation Status
-

  * Implementation focus has been to notice and avoid issues for large script values,
large transactions and very large blocks. One way this is achieved by using chained and reused buffers instead
 of requiring that objects always fit in contiguous bytes and that multiple copies can easily be made.
  * HD Key support is partially done. The base 58 extended key wrappers need finishing.
  * Serializing transactions is partially done.
  * Signing a transaction isn't done yet.
  * The RPC call to submit the transaction is not done.
  * Initial JSON-RPC and ZMQ support. Patterns to follow for extending.
  * No Network support. No connections to peers. No implementation of peer-to-peer protocols.
  * Script interpreter is done except for the last big hill: CHECKSIG, MULTISIG and the Core crap.
  * Just starting work on transaction builder.
  * Unlike the C++ code, there's no support for automatic clearing of memory containing private keys.
  * Just looking at the file counts, there's more undone than done.
  * But there are useful things that can be done with what is done!

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
