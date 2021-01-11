#region Copyright
// Copyright (c) 2020 TonesNotes
// Distributed under the Open BSV software license, see the accompanying file LICENSE.
#endregion
using System;

namespace KzBsv {

    public class KzServiceRequestLog {
        public DateTime When { get; set; }

        public string ServiceEndpoint { get; set; }

        public bool Verified { get; set; }

        public bool Success { get; set; }

        public string ServiceResponse { get; set; }

    }
}
