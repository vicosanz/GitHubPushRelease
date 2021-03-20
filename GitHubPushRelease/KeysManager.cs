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

        public void SaveKeys(List<Keys> keys)
        {
            if (keys is null)
            {
                throw new System.ArgumentNullException(nameof(keys));
            }

            XmlSerializer xmlSerializer = new(keys.GetType());
            TextWriter textWriter = new StreamWriter(_filename);
            xmlSerializer.Serialize(textWriter, keys);
            textWriter.Close();
        }

        public Keys GetToken(string gitUser)
        {
            return Keys.Where(x => string.Compare(x.GitUser, gitUser, true) == 0).FirstOrDefault();
        }
    }
}