[KzBsv](https://github.com/kzbsv/KzBsv)
=

This repository contains the following .NET C#, Visual Studio 2019 compatible projects:

* [KzBsv](https://github.com/kzbsv/KzBsv/tree/master/KzBsv): Bitcoin SV library for .NET Core 3.0

* [bricks](https://github.com/kzbsv/KzBsv/tree/master/bricks): Supporting projects.

  * [KzSecp256k1.Net](https://github.com/MeadowSuite/Secp256k1.Net): A customized fork for access to high performance Secp256k1 C++ library.

* [Tests.KzBsv](https://github.com/tonesnotes/KzBitcoinSV/tree/master/Tests.KzBsv): Unit tests for the KzBsv library.

Changelog
-

**v0.0.1**
* Initial version.

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

Why WinForms vs. Azure or AWS?
-

I wanted to support WinForms because after years or rapid prototyping its the best environment I've found.
Since my goal was to maximize the boost from leveraging the latest tools, its fortunate that Visual Studio 2019 just
picked up the ability to [design WinForms UI's for .NET Core 3.0 within Visual Studio with a temporary hack described here.](https://github.com/dotnet/winforms/blob/master/Documentation/winforms-designer.md)

When writing high performance serialization / deserialization code, having access to the latest C# language features indlucing Index and Range support for Span\<T>
makes a difference. This is possible with the 3.0.0-preview4.19212.13 release of .NET Core which both KzHack and KzBsv are built on.

Why didn't I target browser based, cloud deployed code? Because a lot of the design work that needs to be prototyped and tested is plumbing. Using Javascript
is not going to cut it. Being able to rapidly deploy to user's interacting over the web is not always a priority. This direction is better suited for
prototyping server side code, desktop applications and possibly even non-browser based mobile apps using the Xamarin cross compilation tools.

KzHack Implemementation Status
-

* Access to raw Bitcoin SV blockchain data:
  * JSON-RPC is implemented to retrieve blocks from a Bitcoin SV node backwards from the present until a target date.
  * ZMQ is implemented to keep up-to-date on both new blocks and new transactions.
  * Three tiered access to blocks is implemented: In memory, on disk, and RPC fetching.
  * Reorgs and orphans are supported.
* Toy Wallet Support:
  * You can type or import a pass phrase from the Mnemonic panel.
  * The code creates a master HD key from the pass phrase and does some key derivations.
  * Need to add display of funding address and add transaction tracking to compute balance.
* Really want to get to the point of signing and submitting a transaction.
* Wanted to add support for _unwriter protocols. Specifically managing a folder whose contents are synced to the blockchain. Ideally with an expense per hour budget.
* There's no support for safeguarding private keys or pass phrases. Be careful.

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


