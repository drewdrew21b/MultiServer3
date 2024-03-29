using CustomLogger;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System.Text;
using BackendProject.MiscUtils;

namespace HomeTools.Crypto
{
    internal class LIBSECURE
    {
        // TODO: Is fine most of the time, but at some rare occasions might be wrong on padding.
        public byte[]? InitiateLibsecureXTEACTRBlock(byte[] BlkBytes, byte[] KeyBytes, byte[] m_iv)
        {
            if (KeyBytes.Length == 16 && m_iv.Length == 8 && BlkBytes.Length <= 8)
            {
                byte[] nulledBytes = new byte[BlkBytes.Length];

                // Create the cipher
                IBufferedCipher? cipher = (nulledBytes.Length != 8) ? CipherUtilities.GetCipher("LIBSECUREXTEA/CTR/ZEROBYTEPADDING") : CipherUtilities.GetCipher("LIBSECUREXTEA/CTR/NOPADDING");

                cipher.Init(true, new ParametersWithIV(new KeyParameter(EndianUtils.EndianSwap(KeyBytes)), EndianUtils.EndianSwap(m_iv))); // Bouncy Castle not like padding in decrypt mode with custom data.

                // Encrypt the plaintext
                byte[] ciphertextBytes = new byte[cipher.GetOutputSize(nulledBytes.Length)];
                int ciphertextLength = cipher.ProcessBytes(EndianUtils.EndianSwap(nulledBytes), 0, nulledBytes.Length, ciphertextBytes, 0);
                cipher.DoFinal(ciphertextBytes, ciphertextLength);

                cipher = null;

                if (ciphertextBytes.Length > 8)
                    return new ToolsImpl().Crypt_Decrypt(BlkBytes, EndianUtils.EndianSwap(ciphertextBytes).Take(8).ToArray(), 8);
                else
                    return new ToolsImpl().Crypt_Decrypt(BlkBytes, EndianUtils.EndianSwap(ciphertextBytes), ciphertextBytes.Length);
            }
            else
                LoggerAccessor.LogError("[LIBSECURE] - InitiateLibsecureXTEACTRBlock - Invalid BlkBytes, KeyByes or IV!");

            return null;
        }

        public string MemXOR(string IV, string block, int blocksize)
        {
            StringBuilder? CryptoBytes = new();

            if (blocksize == 2)
            {
                for (int i = 1; i != 0; --i)
                {
                    string BlockIV = IV[..1];
                    string CipherBlock = block[..1];
                    IV = IV[1..];
                    block = block[1..];
                    try
                    {
                        CryptoBytes.Append(VariousUtils.ByteArrayToHexString(VariousUtils.HexStringToByteArray(
                            ((ushort)(Convert.ToUInt16(BlockIV, 16) ^ Convert.ToUInt16(CipherBlock, 16))).ToString("X4"))));
                    }
                    catch (Exception ex)
                    {
                        LoggerAccessor.LogError($"[LIBSECURE] - Error In MemXOR: {ex}");
                    }
                }
            }
            else if (blocksize == 4)
            {
                for (int i = 2; i != 0; --i)
                {
                    string BlockIV = IV[..2];
                    string CipherBlock = block[..2];
                    IV = IV[2..];
                    block = block[2..];
                    try
                    {
                        CryptoBytes.Append(VariousUtils.ByteArrayToHexString(VariousUtils.HexStringToByteArray(
                            ((ushort)(Convert.ToUInt16(BlockIV, 16) ^ Convert.ToUInt16(CipherBlock, 16))).ToString("X4"))));
                    }
                    catch (Exception ex)
                    {
                        LoggerAccessor.LogError($"[LIBSECURE] - Error In MemXOR: {ex}");
                    }
                }
            }
            else if (blocksize == 8)
            {
                for (int i = 4; i != 0; --i)
                {
                    string BlockIV = IV[..4];
                    string CipherBlock = block[..4];
                    IV = IV[4..];
                    block = block[4..];
                    try
                    {
                        CryptoBytes.Append(VariousUtils.ByteArrayToHexString(VariousUtils.HexStringToByteArray(
                            ((ushort)(Convert.ToUInt16(BlockIV, 16) ^ Convert.ToUInt16(CipherBlock, 16))).ToString("X4"))));
                    }
                    catch (Exception ex)
                    {
                        LoggerAccessor.LogError($"[LIBSECURE] - Error In MemXOR: {ex}");
                    }
                }
            }
            else if (blocksize == 16)
            {
                for (int i = 8; i != 0; --i)
                {
                    string BlockIV = IV[..8];
                    string CipherBlock = block[..8];
                    IV = IV[8..];
                    block = block[8..];
                    try
                    {
                        CryptoBytes.Append(VariousUtils.ByteArrayToHexString(VariousUtils.HexStringToByteArray(
                            (Convert.ToUInt32(BlockIV, 16) ^ Convert.ToUInt32(CipherBlock, 16)).ToString("X8"))));
                    }
                    catch (Exception ex)
                    {
                        LoggerAccessor.LogError($"[LIBSECURE] - Error In MemXOR: {ex}");
                    }
                }
            }

            return CryptoBytes.ToString();
        }
    }
}
