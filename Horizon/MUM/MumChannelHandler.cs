﻿using BackendProject.Horizon.RT.Common;
using CustomLogger;
using Horizon.MEDIUS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;

namespace Horizon.MUM
{
    public class MumChannelHandler
    {
        public static string JsonSerializeChannel(Channel channel)
        {
            return JsonConvert.SerializeObject(channel, Formatting.Indented, new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
            });
        }

        public static string XMLSerializeChannel(Channel channel)
        {
            return JsonConvert.DeserializeXmlNode(JsonConvert.SerializeObject(channel, new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
            }), "Channel")?.OuterXml ?? "<Channel></Channel>";
        }

        public static string? JsonSerializeChannelsList(bool encrypt)
        {
            if (encrypt)
                return "<Secure>" + BackendProject.CryptoUtils.AESCTR256EncryptDecrypt.InitiateCTRBufferTobase64String(JsonConvert.SerializeObject(MediusClass.Manager.GetAllChannels(), Formatting.Indented, new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                    Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
                }), Convert.FromBase64String(HorizonServerConfiguration.MediusAPIKey), MumUtils.ConfigIV) + "</Secure>";
            else
                return JsonConvert.SerializeObject(MediusClass.Manager.GetAllChannels(), Formatting.Indented, new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                    Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
                });
        }

        public static string? XMLSerializeChannelsList(bool encrypt)
        {
            if (encrypt)
                return "<Secure>" + BackendProject.CryptoUtils.AESCTR256EncryptDecrypt.InitiateCTRBufferTobase64String(JsonConvert.DeserializeXmlNode(new JObject(new JProperty("ChannelsList", JToken.Parse(JsonConvert.SerializeObject(MediusClass.Manager.GetAllChannels(), new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                    Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
                })))).ToString(), "Root")?.OuterXml ?? "<Root></Root>", Convert.FromBase64String(HorizonServerConfiguration.MediusAPIKey), MumUtils.ConfigIV) + "</Secure>";
            else
                return JsonConvert.DeserializeXmlNode(new JObject(new JProperty("ChannelsList", JToken.Parse(JsonConvert.SerializeObject(MediusClass.Manager.GetAllChannels(), new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                    Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
                })))).ToString(), "Root")?.OuterXml ?? "<Root></Root>";
        }

        public static string GetCRC32ChannelsList()
        {
            // No need to protect the CRC list, nothing critical in here.

            string XMLData = "<Root>";

            foreach (Channel channel in MediusClass.Manager.GetAllChannels())
            {
                XMLData += $"<CRC32 name=\"{channel.Name}\">{new BackendProject.MiscUtils.Crc32Utils().Get(Encoding.UTF8.GetBytes(channel.Name + XMLSerializeChannel(channel))):X}</CRC32>";
            }

            return XMLData + "</Root>";
        }

        public static int GetIndexOfLocalChannelByNameAndAppId(string channelName, int AppId)
        {
            try
            {
                // If a matching channel is found, return the index of the list where it was found.
                int index = MediusClass.Manager.GetAllChannels().FindIndex(channel => channel.Name == channelName && channel.ApplicationId == AppId);

                if (index != -1)
                    return index;
            }
            catch (Exception e)
            {
                LoggerAccessor.LogError($"[MUM] - GetIndexOfLocalChannelByNameAndAppId thrown an exception: {e}");
            }

            // If no matching channel is found, return -1
            return -1;
        }

        public static int GetIndexOfLocalChannelByIdAndAppId(int channelId, int AppId)
        {
            try
            {
                // If a matching channel is found, return the index of the list where it was found.
                int index = MediusClass.Manager.GetAllChannels().FindIndex(channel => channel.Id == channelId && channel.ApplicationId == AppId);

                if (index != -1)
                    return index;
            }
            catch (Exception e)
            {
                LoggerAccessor.LogError($"[MUM] - GetIndexOfLocalChannelByIdAndAppId thrown an exception: {e}");
            }

            // If no matching channel is found, return -1
            return -1;
        }

        public static Channel? GetRemoteChannelByName(Channel channel, IPAddress ClientIp)
        {
            try
            {
                if (MediusClass.MUMLocalServersAccessList.Count > 0)
                {
                    foreach (KeyValuePair<string, string> kvp in MediusClass.MUMLocalServersAccessList)
                    {
                        string? RemoteChannelsList = MumClient.GetServerResult(kvp.Key, 10076, "GetChannelsJson", kvp.Value);
                        if (!string.IsNullOrEmpty(RemoteChannelsList))
                        {
                            List<Channel>? ConvertedChannelsLists = JsonConvert.DeserializeObject<List<Channel>>(RemoteChannelsList, new JsonSerializerSettings
                            {
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                                Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
                            });

                            if (ConvertedChannelsLists != null)
                            {
                                Channel? matchingChannel = ConvertedChannelsLists.FirstOrDefault(x => x.Name == channel.Name && x.ApplicationId == channel.ApplicationId);

                                if (matchingChannel != null)
                                    return matchingChannel;
                            }
                        }
                    }
                }
            }
            catch (InvalidOperationException invalidOperationException)
            {
                if (invalidOperationException.Message.Contains("Sequence contains no elements"))
                    LoggerAccessor.LogWarn($"[MUM] - GetRemoteChannelByName No matching channel found in any server.");
                else
                    LoggerAccessor.LogError($"[MUM] - GetRemoteChannelByName thrown an InvalidOperationException: {invalidOperationException}");
            }
            catch (Exception e)
            {
                LoggerAccessor.LogError($"[MUM] - GetRemoteChannelByName thrown an exception: {e}");
            }

            return null;
        }

        public static Channel? GetRemoteChannelById(int WorldId, int Appid, IPAddress ClientIp)
        {
            try
            {
                if (MediusClass.MUMLocalServersAccessList.Count > 0)
                {
                    foreach (KeyValuePair<string, string> kvp in MediusClass.MUMLocalServersAccessList)
                    {
                        string? RemoteChannelsList = MumClient.GetServerResult(kvp.Key, 10076, "GetChannelsJson", kvp.Value);
                        if (!string.IsNullOrEmpty(RemoteChannelsList))
                        {
                            List<Channel>? ConvertedChannelsLists = JsonConvert.DeserializeObject<List<Channel>>(RemoteChannelsList, new JsonSerializerSettings
                            {
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                                Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
                            });

                            if (ConvertedChannelsLists != null)
                            {
                                Channel? matchingChannel = ConvertedChannelsLists.FirstOrDefault(x => x.Id == WorldId && x.ApplicationId == Appid);

                                if (matchingChannel != null)
                                    return matchingChannel;
                            }
                        }
                    }
                }
            }
            catch (InvalidOperationException invalidOperationException)
            {
                if (invalidOperationException.Message.Contains("Sequence contains no elements"))
                    LoggerAccessor.LogWarn($"[MUM] - GetRemoteChannelById No matching channel found in any server.");
                else
                    LoggerAccessor.LogError($"[MUM] - GetRemoteChannelById thrown an InvalidOperationException: {invalidOperationException}");
            }
            catch (Exception e)
            {
                LoggerAccessor.LogError($"[MUM] - GetRemoteChannelById thrown an exception: {e}");
            }

            return null;
        }

        public static Channel? GetLeastPopulatedRemoteChannel(int Appid, IPAddress ClientIp)
        {
            try
            {
                if (MediusClass.MUMLocalServersAccessList.Count > 0)
                {
                    foreach (KeyValuePair<string, string> kvp in MediusClass.MUMLocalServersAccessList)
                    {
                        string? RemoteChannelsList = MumClient.GetServerResult(kvp.Key, 10076, "GetChannelsJson", kvp.Value);
                        if (!string.IsNullOrEmpty(RemoteChannelsList))
                        {
                            List<Channel>? ConvertedChannelsLists = JsonConvert.DeserializeObject<List<Channel>>(RemoteChannelsList, new JsonSerializerSettings
                            {
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                                Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
                            });

                            if (ConvertedChannelsLists != null)
                            {
                                Channel? matchingChannel = ConvertedChannelsLists
                                        .Where(channel => channel.ApplicationId == Appid)
                                        .OrderBy(channel => channel.PlayerCount)
                                        .FirstOrDefault();

                                if (matchingChannel != null)
                                    return matchingChannel;
                            }
                        }
                    }
                }
            }
            catch (InvalidOperationException invalidOperationException)
            {
                if (invalidOperationException.Message.Contains("Sequence contains no elements"))
                    LoggerAccessor.LogWarn($"[MUM] - GetLeastPopulatedRemoteChannel No matching channel found in any server.");
                else
                    LoggerAccessor.LogError($"[MUM] - GetLeastPopulatedRemoteChannel thrown an InvalidOperationException: {invalidOperationException}");
            }
            catch (Exception e)
            {
                LoggerAccessor.LogError($"[MUM] - GetLeastPopulatedRemoteChannel thrown an exception: {e}");
            }

            return null;
        }

        public static IEnumerable<Channel>? GetRemoteChannelListFiltered(int appId, int pageIndex, int pageSize, ChannelType type, ulong FieldMask1, ulong FieldMask2, ulong FieldMask3, ulong FieldMask4, MediusLobbyFilterMaskLevelType filterMaskLevelType, IPAddress ClientIp)
        {
            try
            {
                if (MediusClass.MUMLocalServersAccessList.Count > 0)
                {
                    List<Channel> ChannelsLists = new();

                    foreach (KeyValuePair<string, string> kvp in MediusClass.MUMLocalServersAccessList)
                    {
                        string? RemoteChannelsList = MumClient.GetServerResult(kvp.Key, 10076, "GetChannelsJson", kvp.Value);
                        if (!string.IsNullOrEmpty(RemoteChannelsList))
                        {
                            List<Channel>? ConvertedChannelsLists = JsonConvert.DeserializeObject<List<Channel>>(RemoteChannelsList, new JsonSerializerSettings
                            {
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                                Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
                            });

                            if (ConvertedChannelsLists != null)
                            {
                                foreach (Channel channel in ConvertedChannelsLists
                                        .Where(x => x.Type == type &&
                                            x.ApplicationId == appId &&
                                            x.GenericField1 == FieldMask1 &&
                                            x.GenericField2 == FieldMask2 &&
                                            x.GenericField3 == FieldMask3 &&
                                            x.GenericField4 == FieldMask4 &&
                                            x.GenericFieldLevel == (MediusWorldGenericFieldLevelType)filterMaskLevelType)
                                        .Skip((pageIndex - 1) * pageSize)
                                        .Take(pageSize))
                                {
                                    ChannelsLists.Add(channel);
                                }
                            }
                        }
                    }

                    if (ChannelsLists.Count > 0)
                        return ChannelsLists;
                }
            }
            catch (InvalidOperationException invalidOperationException)
            {
                if (invalidOperationException.Message.Contains("Sequence contains no elements"))
                    LoggerAccessor.LogWarn($"[MUM] - GetRemoteChannelListFiltered No matching channel found in any server.");
                else
                    LoggerAccessor.LogError($"[MUM] - GetRemoteChannelListFiltered thrown an InvalidOperationException: {invalidOperationException}");
            }
            catch (Exception e)
            {
                LoggerAccessor.LogError($"[MUM] - GetRemoteChannelListFiltered thrown an exception: {e}");
            }

            return null;
        }

        public static IEnumerable<Channel>? GetRemoteChannelsList(int appId, int pageIndex, int pageSize, ChannelType type, IPAddress ClientIp)
        {
            try
            {
                if (MediusClass.MUMLocalServersAccessList.Count > 0)
                {
                    List<Channel> ChannelsLists = new();

                    foreach (KeyValuePair<string, string> kvp in MediusClass.MUMLocalServersAccessList)
                    {
                        string? RemoteChannelsList = MumClient.GetServerResult(kvp.Key, 10076, "GetChannelsJson", kvp.Value);
                        if (!string.IsNullOrEmpty(RemoteChannelsList))
                        {
                            List<Channel>? ConvertedChannelsLists = JsonConvert.DeserializeObject<List<Channel>>(RemoteChannelsList, new JsonSerializerSettings
                            {
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                                Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
                            });

                            if (ConvertedChannelsLists != null)
                            {
                                foreach (Channel channel in ConvertedChannelsLists
                                         .Where(x => x.Type == type &&
                                             x.ApplicationId == appId)
                                         .Skip((pageIndex - 1) * pageSize)
                                         .Take(pageSize))
                                {
                                    ChannelsLists.Add(channel);
                                }
                            }
                        }
                    }

                    if (ChannelsLists.Count > 0)
                        return ChannelsLists;
                }
            }
            catch (InvalidOperationException invalidOperationException)
            {
                if (invalidOperationException.Message.Contains("Sequence contains no elements"))
                    LoggerAccessor.LogWarn($"[MUM] - GetRemoteChannelsList No matching channel found in any server.");
                else
                    LoggerAccessor.LogError($"[MUM] - GetRemoteChannelsList thrown an InvalidOperationException: {invalidOperationException}");
            }
            catch (Exception e)
            {
                LoggerAccessor.LogError($"[MUM] - GetRemoteChannelsList thrown an exception: {e}");
            }

            return null;
        }

        public static uint GetRemoteChannelCount(ChannelType type, int appId, IPAddress ClientIp)
        {
            try
            {
                if (MediusClass.MUMLocalServersAccessList.Count > 0)
                {
                    uint totalCount = 0;

                    foreach (KeyValuePair<string, string> kvp in MediusClass.MUMLocalServersAccessList)
                    {
                        string? RemoteChannelsList = MumClient.GetServerResult(kvp.Key, 10076, "GetChannelsJson", kvp.Value);
                        if (!string.IsNullOrEmpty(RemoteChannelsList))
                        {
                            List<Channel>? ConvertedChannelsLists = JsonConvert.DeserializeObject<List<Channel>>(RemoteChannelsList, new JsonSerializerSettings
                            {
                                PreserveReferencesHandling = PreserveReferencesHandling.Objects | PreserveReferencesHandling.Arrays,
                                Converters = { new BackendProject.MiscUtils.JsonIPConverterUtils() }
                            });

                            if (ConvertedChannelsLists != null)
                            {
                                // Add the count of matching channels to the total count
                                totalCount += (uint)ConvertedChannelsLists
                                    .Where(x => x.Type == type && x.ApplicationId == appId).Count();
                            }
                        }
                    }

                    return totalCount;
                }
            }
            catch (InvalidOperationException invalidOperationException)
            {
                if (invalidOperationException.Message.Contains("Sequence contains no elements"))
                    LoggerAccessor.LogWarn($"[MUM] - GetRemoteChannelCount No matching channel found in any server.");
                else
                    LoggerAccessor.LogError($"[MUM] - GetRemoteChannelCount thrown an InvalidOperationException: {invalidOperationException}");
            }
            catch (Exception e)
            {
                LoggerAccessor.LogError($"[MUM] - GetRemoteChannelCount thrown an exception: {e}");
            }

            return 0;
        }
    }
}
