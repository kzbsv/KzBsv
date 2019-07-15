using System;
using System.Collections.Generic;
using System.Text;

namespace KzBsv
{
    public class KzElectrumSv
    {
        public static KzExtPrivKey GetMasterPrivKey(string seed, string seedExtension = null) =>
            KzExtPrivKey.Master(KzHashes.pbkdf2_hmac_sha512(seed.UTF8ToBytes(), $"electrum{seedExtension}".UTF8ToBytes(), 2048).Span);
    }
}
