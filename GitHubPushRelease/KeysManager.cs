using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace GitHubPushRelease
{
    public class KeysManager
    {
        public string _filename { get; }

        public KeysManager(string filename)
        {
            _filename = filename;
        }

        internal List<Keys> _cacheKeys;

        public List<Keys> Keys
        {
            get
            {
                if (_cacheKeys is null)
                {
                    _cacheKeys = new();
                    if (File.Exists(_filename))
                    {
                        XmlSerializer xmlSerializer = new(_cacheKeys.GetType());
                        FileStream fileStream = new(_filename, FileMode.Open);
                        XmlTextReader xmlTextReader = new(fileStream);
                        if (xmlSerializer.CanDeserialize(xmlTextReader))
                        {
                            _cacheKeys = xmlSerializer.Deserialize(xmlTextReader) as List<Keys>;
                        }
                        fileStream.Close();
                    }
                }
                return _cacheKeys;
            }
        }

        public void SaveKeys()
        {
            if (Keys is null)
            {
                throw new Exception("Keys are not loaded");
            }

            XmlSerializer xmlSerializer = new(Keys.GetType());
            TextWriter textWriter = new StreamWriter(_filename);
            xmlSerializer.Serialize(textWriter, Keys);
            textWriter.Close();
        }

        public Keys GetToken(string gitUser)
        {
            return Keys.Where(x => string.Equals(x.GitUser, gitUser, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public void AddorUpdateKey(Keys newkey)
        {
            if (Keys is null)
            {
                throw new Exception("Keys are not loaded");
            }

            Keys.RemoveAll(x => string.Equals(x.GitUser, newkey.GitUser, StringComparison.InvariantCultureIgnoreCase));
            Keys.Add(newkey);
        }
    }
}