// Copyright (C) 2012 David W. Jeske
// Donated to the public domain.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using Microsoft.Win32;

//
// http://msdn.microsoft.com/en-us/library/system.windows.forms.application.userappdataregistry.aspx
// http://msdn.microsoft.com/en-us/library/ms973902.aspx#persistappsettnet_topic3
// http://www.switchonthecode.com/tutorials/csharp-snippet-tutorial-editing-the-windows-registry
//
// permissions
// http://www.codeguru.com/forum/archive/index.php/t-451257.html


namespace SimpleScene {
    // [assembly: AssemblyInformationalVersion("1.0")]
    // http://www.pcreview.co.uk/forums/using-userappdataregistry-t1226335.html
    public class Prefs {
        
        public static bool prefExists(string key) {

            RegistryKey myKey = Application.UserAppDataRegistry;

            object value = myKey.GetValue(key);
            return value != null;
        }

        public static void setPref<T>(string key, T value) {

			RegistryKey myKey = Application.UserAppDataRegistry;
                        
            myKey.SetValue(key,value);
            myKey.Flush();

        }

        public static T getPref<T>(string key) {

            RegistryKey myKey = Application.UserAppDataRegistry;

            return (T)myKey.GetValue(key);
        }

        public static T getPref<T>(string key, T defaultValue) {
            RegistryKey myKey = Application.UserAppDataRegistry;
            
            return (T)myKey.GetValue(key, defaultValue);
        }
    }
}
