// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

// Copyright (C) 2012, David W. Jeske

namespace WavefrontOBJViewer
{

    public class SSNoSuchAssetException : Exception {
        public string basepath;
        public string resource_name;
        public ISSAssetArchiveHandler[] handlers_arr;

        public override string ToString() {
            string handler_list = "";
            foreach (var handler in handlers_arr) {
                handler_list += ":" + handler.ToString();
            }

            return String.Format("[SSNoSuchAssetException:{0},{1},{2}",
                basepath, resource_name, handler_list);
        }

        public SSNoSuchAssetException(string basepath, string resource_name, ISSAssetArchiveHandler[] handlers_arr) {
            this.basepath = basepath;
            this.resource_name = resource_name;
            this.handlers_arr = handlers_arr;
        }
    }

    public class SSAssetHandlerNotFoundException : Exception {
        public ISSAssetArchiveHandler handler;
        public string basepath;
        public string resource_name;

        public override string ToString() {
            return String.Format("[{0}:{1}:{2}]", handler.ToString(), basepath, resource_name);
        }

        public SSAssetHandlerNotFoundException(ISSAssetArchiveHandler handler, string basepath, string resource_name) {
            this.handler = handler;
            this.basepath = basepath;
            this.resource_name = resource_name;
        }
    }

    public class SSAssetHandlerLoadException : Exception {
        public ISSAssetArchiveHandler handler;
        public string basepath;
        public string resource_name;
        public Exception base_exception;

        public override string ToString() {
            return String.Format("[{0}:{1}:{2} threw Exception {3}]",
                handler.ToString(), basepath, resource_name, base_exception.ToString());
        }


        public SSAssetHandlerLoadException(ISSAssetArchiveHandler handler, string basepath, string resource_name, Exception e) {
            this.handler = handler;
            this.basepath = basepath;
            this.resource_name = resource_name;
            this.base_exception = e;
        }
    }

    public class SSAssetArchiveHandler_FileSystem : ISSAssetArchiveHandler {
        string basepath;
        public SSAssetArchiveHandler_FileSystem(string basepath) {
            this.basepath = basepath;
        }
        public override string ToString() {
            return String.Format("SSAssetArchiveHandler_FileSystem {0} ({1})",
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), this.basepath)),             
                this.basepath);
        }

        public Stream openResource(string basename, string resource_name) {
            string fullpath = Path.Combine(this.basepath, basename, resource_name);
            if (!File.Exists(fullpath)) {
                throw new SSAssetHandlerNotFoundException(this, basename, resource_name);
            } else {
                try {
                    return File.Open(fullpath, FileMode.Open, FileAccess.Read, FileShare.Read);
                } catch (Exception e) {
                    throw new SSAssetHandlerLoadException(this, basepath, resource_name, e);
                }
            }
        }
    }

    public interface ISSAssetArchiveHandler {
        Stream openResource(string basename, string resource_name);
        string ToString();
    }

    public class SSAssetManagerContext {
        SSAssetManager mgr;
        string basepath;

        public SSAssetManagerContext(SSAssetManager mgr, string basepath) {
            this.mgr = mgr;
            this.basepath = basepath;
        }

        public Stream openResource(string resource_name) {
            return this.mgr.openResource(this.basepath, resource_name);
        }


        public string fullHandlePathForResource(string resource_name) {
            return Path.Combine(basepath, resource_name);
        }

    }

    public class SSAssetManager {
        public static SSAssetManager mgr = new SSAssetManager();
        List<ISSAssetArchiveHandler> handlers = new List<ISSAssetArchiveHandler>();

        public void addAssetArchive(ISSAssetArchiveHandler handler) {
            lock (this) {
                handlers.Add(handler);
            }
        }

        public string resourceCachePath(string basepath, string resource_name) {
            return Path.Combine(basepath, resource_name);
        }

        public Stream openResource(string basepath, string resource_name) {                           
            ISSAssetArchiveHandler[] handlers_arr;

            // make sure we are thread-safe on asset handler registration (though this really sholdn't happen)
            lock (this) { handlers_arr = this.handlers.ToArray(); }

            if (handlers_arr.Length == 0) {
                throw new Exception("SSAssetManager: no handlers available for load");
            }

            foreach (ISSAssetArchiveHandler handler in handlers_arr) {
                try {                
                    return handler.openResource(basepath, resource_name);                    
                } catch (Exception e) {
                    
                }                
            }

            // no asset found
            throw new SSNoSuchAssetException(basepath, resource_name, handlers_arr);
            
        }
        public SSAssetManagerContext getContextForResource(string fullpath) {
            string basepath = Path.Combine(fullpath, "../");
            return new SSAssetManagerContext(this, basepath);
        }
        public SSAssetManagerContext getContext(string basepath) {
            return new SSAssetManagerContext(this, basepath);
        }
    }
}
