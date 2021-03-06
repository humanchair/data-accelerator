﻿// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Contract.Exception;
using DataX.Utilities.KeyVault;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DataX.Flow.Common
{
    public static class Helper
    {
        private static readonly Regex _ConnectionStringFormat = new Regex("^endpoint=([^;]*);username=([^;]*);password=(.*)$");

        /// <summary>
        /// Replaces tokens in the templates
        /// </summary>
        /// <param name="template"></param>
        /// <param name="values"></param>
        /// <returns>Translated template</returns>
        public static string TranslateOutputTemplate(string template, Dictionary<string, string> values)
        {
            foreach (var kvp in values)
            {
                template = template.Replace($"<@{kvp.Key}>", kvp.Value);
            }

            return template;
        }     

        /// <summary>
        /// Checks if the value is a keyvault and if it is, gets the value from a keyvault
        /// Otherwise, returns as is
        /// </summary>
        /// <param name="value"></param>
        /// <returns>value or value from secret</returns>
        public static string GetSecretFromKeyvaultIfNeeded(string value)
        {
            if (IsKeyVault(value))
            {
                return KeyVault.GetSecretFromKeyvault(value);
            }

            return value;
        }

        /// <summary>
        /// Checks if it is a secret
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true if it is a secret, otherwise false</returns>
        public static bool IsKeyVault(string value)
        {
            return value.StartsWith(GetKeyValutNamePrefix());
        }

        /// <summary>
        /// Composes a keyVault uri
        /// </summary>
        /// <param name="keyvaultName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="postfix"></param>
        /// <returns>keyVault uri</returns>
        public static string GetKeyVaultName(string keyvaultName, string key, string value = "", bool postfix = true)
        {
            if (postfix)
            {
                key = key + $"-{Helper.GetHashCode(value)}";
            }

            return $"{GetKeyValutNamePrefix()}{keyvaultName}/{key}";
        }

        /// <summary>
        /// Generates a new secret and adds to a list. And the items in the list will be stored in a keyvault 
        /// </summary>
        /// <param name="keySecretList"></param>
        /// <param name="keyvaultName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="postfix"></param>
        /// <returns>secret name</returns>
        public static string GenerateNewSecret(Dictionary<string, string> keySecretList, string keyvaultName, string key, string value, bool postfix = true)
        {
            key = GetKeyVaultName(keyvaultName, key, value, postfix);

            keySecretList.TryAdd(key, value);

            return key;
        }

        /// <summary>
        /// Get the prefix for keyvault uri
        /// </summary>
        /// <returns>the prefix for keyvault uri</returns>
        public static string GetKeyValutNamePrefix()
        {
            return "keyvault://";
        }     
    
        
        /// <summary>
        /// Parses the eventhub namespace from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>eventhub namespace</returns>
        public static string ParseEventHubNamespace(string connectionString)
        {
            var key = "Endpoint";
            var map = ConnectionStringToKeyValueMap(connectionString);
            if (map.ContainsKey(key))
            {
                var value = map[key];
                var match = Regex.Match(value, @"sb:\/\/(.*?)\.servicebus\.windows\.net");
                if (match.Success && match.Groups.Count == 2)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;            
        }

        /// <summary>
        /// Parses the eventhub from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>eventhub</returns>
        public static string ParseEventHub(string connectionString)
        {
            var key = "EntityPath";
            var map = ConnectionStringToKeyValueMap(connectionString);
            return map.ContainsKey(key) ? map[key] : null;
        }

        /// <summary>
        /// Parses the eventhub accesskey from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>eventhub accesskey</returns>
        public static string ParseEventHubAccessKey(string connectionString)
        {
            var key = "SharedAccessKey";
            var map = ConnectionStringToKeyValueMap(connectionString);
            return map.ContainsKey(key) ? map[key] : null;
        }
    

        /// <summary>
        /// Parses the eventhub policy from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>eventhub policy </returns>
        public static string ParseEventHubPolicyName(string connectionString)
        {
            var key = "SharedAccessKeyName";
            var map = ConnectionStringToKeyValueMap(connectionString);
            return map.ContainsKey(key) ? map[key] : null;
        }

        /// <summary>
        /// Parses the cosmosDB endpoint from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>cosmosDB endpoint</returns>
        public static string ParseCosmosDBEndPoint(string connectionString)
        {
            string matched;
            try
            {
                matched = Regex.Match(connectionString, @"(?<===@)(.*)(?=:10255)").Value;
            }
            catch (Exception)
            {
                return "The connectionString does not have PolicyName";
            }

            return matched;
        }

        /// <summary>
        /// Parses the cosmosDB username from connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>cosmosDB username</returns>
        public static string ParseCosmosDBUserNamePassword(string connectionString)
        {
            string matched;
            try
            {
                matched = Regex.Match(connectionString, @"(?<=//)(.*)(?=@)").Value;
            }
            catch (Exception)
            {
                return "The connectionString does not have username/password";
            }

            return matched;
        }         

        /// <summary>
        /// Generates a hashcode for the input
        /// </summary>
        /// <param name="value"></param>
        /// <returns>hashcode for the input</returns>
        public static string GetHashCode(string value)
        {
            HashAlgorithm hash = SHA256.Create();
            var hashedValue = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
            return BitConverter.ToString(hashedValue).Replace("-", string.Empty).Substring(0, 32);
        }
      

        /// <summary>
        /// Generates a map for the input connectionstring
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>map for the connectionstring</returns>
        private static IDictionary<string, string> ConnectionStringToKeyValueMap(string connectionString)
        {
           var keyValueMap = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                var keyValues = connectionString.Split(';');

                foreach (var keyValue in keyValues)
                {
                    var keyValuePair = keyValue.Split(new char[] { '=' }, 2);
                    if (keyValuePair.Length == 2)
                    {
                        var key = keyValuePair[0];
                        var value = keyValuePair[1];
                        if (keyValueMap.ContainsKey(key))
                        {
                            keyValueMap[key] = value;
                        }
                        else
                        {
                            keyValueMap.Add(key, value);
                        }
                    }
                }
            }

            return keyValueMap;
        }

        /// <summary>
        /// PathResolver resolves the keyvault uri and gets the real path 
        /// </summary>
        /// <param name="path">path</param>
        /// <returns>Returns a string </returns>
        public static string PathResolver(string path)
        {
            if (path != null && Config.Utility.KeyVaultUri.IsSecretUri(path))
            {
                Regex r = new Regex(@"^((keyvault:?):\/\/)?([^:\/\s]+)(\/)(.*)?", RegexOptions.IgnoreCase);
                var keyvault = string.Empty;

                var secret = string.Empty;
                MatchCollection match = r.Matches(path);
                if (match != null && match.Count > 0)
                {
                    foreach (Match m in match)
                    {
                        keyvault = m.Groups[3].Value.Trim();
                        secret = m.Groups[5].Value.Trim();
                    }
                }
                var secretUri = KeyVault.GetSecretFromKeyvault(keyvault, secret);

                return secretUri;
            }
            return path;
        }

        /// <summary>
        /// ParseConnectionString the connection string to extract username and password
        /// </summary>
        /// <param name="connectionString">connectionString</param>
        /// <returns>SparkConnectionInfo object</returns>        
        public static SparkConnectionInfo ParseConnectionString(string connectionString)
        {
            if (connectionString == null)
            {
                throw new GeneralException($"connection string for livy client cannot be null");
            }

            var match = _ConnectionStringFormat.Match(connectionString);
            if (match == null || !match.Success)
            {
                throw new GeneralException($"cannot parse connection string to access livy service");
            }

            return new SparkConnectionInfo()
            {
                Endpoint = match.Groups[1].Value,
                UserName = match.Groups[2].Value,
                Password = match.Groups[3].Value
            };
        }        
    }

    /// <summary>
    /// Class object for the connection info
    /// </summary>
    public class SparkConnectionInfo
    {
        public string Endpoint { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
