using KzBsv;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Tests.KzBsv.Network {
    public class RpcTests {

        public KzRpcClient GetKzRpc() {
            // Use command setx KZRPCAUTH "{username}:{password}"  to set this environment variable. Then restart Visual Studio to access.
            var auth = Environment.GetEnvironmentVariable("KZRPCAUTH");
            var addr = Environment.GetEnvironmentVariable("KZRPCADDR");
            var uri = new Uri(addr);

            var kzrpc = new KzRpcClient(auth, uri);
            return kzrpc;
        }

        [Fact]
        public void GetPeerInfoTest() {
            var rpc = GetKzRpc();
            var pi = rpc.GetPeerInfo();
        }
    }
}
