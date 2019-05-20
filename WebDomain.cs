using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.ComponentModel;
namespace ProxyServer
{
    
    public class WebDomain
    {
        /// <exception cref="ArgumentException"></exception>
        public WebDomain(string domain)
        {
            if (string.IsNullOrEmpty(domain) || string.IsNullOrWhiteSpace(domain))
                throw new ArgumentException("Argument is null or empty or only consisting of white-space characters");

            string[] parsed = DomainParser(domain);

            SubDomain = parsed[0].ToLowerInvariant();
            Name = parsed[1].ToLowerInvariant();
            Extension = parsed[2].ToLowerInvariant();
        }

        public WebDomain(string subdomain, string name, string extension)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("name is null or empty or only consisting of white-space characters");
            if (string.IsNullOrEmpty(extension) || string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("extension is null or empty or only consisting of white-space characters");
            if (string.IsNullOrEmpty(subdomain))
                throw new ArgumentException("subdomain is null or empty");

            if (string.IsNullOrWhiteSpace(subdomain)) SubDomain = string.Empty;
            Name = name.ToLowerInvariant();
            Extension = extension.ToLowerInvariant();
        }

        protected string[] DomainParser(string domain)
        {
            string[] parsed = new string[3] { string.Empty, string.Empty, string.Empty };
            int[] DotPosiions = new int[2];


            DotPosiions[0] = domain.IndexOf('.');
            DotPosiions[1] = domain.IndexOf('.', DotPosiions[0] + 1);

            if (DotPosiions[0] < 0) throw new ArgumentException("Invalid format of argument");
            else if (DotPosiions[1] < 0)
            {
                parsed[0] = string.Empty;
                parsed[1] = domain.Substring(0, DotPosiions[0]);
                parsed[2] = domain.Substring(DotPosiions[0] + 1);
            }
            else
            {
                parsed[0] = domain.Substring(0, DotPosiions[0]);
                parsed[1] = domain.Substring(DotPosiions[0] + 1, DotPosiions[1] - DotPosiions[0] - 1);
                parsed[2] = domain.Substring(DotPosiions[1] + 1);
            }

            return parsed;
        }

        public string SubDomain { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;

        public string Domain
        {
            get
            {
                if(SubDomain == string.Empty)
                    return string.Format("{1}.{2}", SubDomain, Name, Extension);
                else
                    return string.Format("{0}.{1}.{2}", SubDomain, Name, Extension);
            }
        }

        public override bool Equals(object obj)
        {
            WebDomain y = obj as WebDomain;

            if (obj == null) return false;

            return Name == y.Name && Extension == y.Extension && SubDomain == y.SubDomain;
        }

        public override string ToString()
        {
            return Domain;
        }
    }

    public class DefaultWebDomainComparer : IComparer<WebDomain>
    {
        public int Compare(WebDomain x, WebDomain y)
        {
            int i = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            if (i == 0)
            {
                int k = string.Compare(x.Extension, y.Extension, StringComparison.Ordinal);
                if (k == 0)
                {
                    return string.Compare(x.SubDomain, y.SubDomain, StringComparison.Ordinal);
                }
                return k;
            }
            return i;
        }
    }
    public class WebDomainsView
    {

        private ObservableCollection<WebDomain> _items = null;
        public ObservableCollection<WebDomain> Items
        {
            get
            {
                return _items;
            }
            set
            {
                Items = value;
            }
            
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public WebDomainsView(ObservableCollection<WebDomain> items)
        {
            _items = items;
        }

        public WebDomainsView()
        {
            _items = new ObservableCollection<WebDomain>();
        }

        public void Add(WebDomain item)
        {
            if (_items.IndexOf(item) < 0)
                lock (this)
                {
                    _items.Add(item);
                }
        }

        public bool Remove(WebDomain item)
        {
            lock (Items)
            {
                return Items.Remove(item);
            }
        }
        public void RemoveAt(int i)
        {
            lock (Items)
            {
                Items.RemoveAt(i);
            }
        }
        public void Clear()
        {
            lock (Items)
            {
                _items.Clear();

            }
        }
    }
}
