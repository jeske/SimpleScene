// Copyright(C) David W. Jeske, 2012, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

// This is an AssetManager, it's job is to abstract away the means of turning an asset
// path into a File-Stream, so an asset can be stored in some form of archive, such as a ZIP, PACK, WAD, etc.
// 
// It also separates the process of finding an asset from the process of opening the assset into a stream,
// so an opaque SSAssetItem handle can be passed around and stored, and Open()ed at any time, and repeatedly
// whenever it is needed.
//
// Common usage looks something like this:
// 
//  // 1. register an asset archive with the manager, all registered archives are searched in
//  //    order for any assets requested
// 	SSAssetManager.mgr.addAssetArchive(new SSAssetArchiveHandler_FileSystem("./Assets"));
//
//  // 2. open an asset context. this is like a directory, except that when opening things
//  //    in this directory, all registered asset archives will be searched (in order) for a 
//  //    matching asset
// 	SSAssetManagerContext ctx = SSAssetManager.mgr.getContext("./drone2/");
//
//  // 3. 
// ctx.getAsset(resource_name).Open()


namespace SimpleScene
{

	#region Interfaces
	public interface ISSAssetArchiveHandler {
		bool resourceExists (string resource_name);
		Stream openResource(string resource_name);
	}
	#endregion


	#region Exceptions
	public abstract class SSAssetException : Exception {} 
	public class SSNoSuchAssetException : SSAssetException {
		public readonly string resource_name;
		public readonly ISSAssetArchiveHandler[] handlers_arr;

		public override string ToString() {
			string handler_list = "";
			foreach (var handler in handlers_arr) {
				handler_list += ":" + handler.ToString();
			}

			return String.Format("[SSNoSuchAssetException:{0},{1}",
			                     resource_name, handler_list);
		}

		public SSNoSuchAssetException(string resource_name, ISSAssetArchiveHandler[] handlers_arr) {
			this.resource_name = resource_name;
			this.handlers_arr = handlers_arr;
		}
	}

	public class SSAssetHandlerNotFoundException : SSAssetException {
        public readonly ISSAssetArchiveHandler handler;
        public readonly string resource_name;

        public override string ToString() {
            return String.Format("[{0}:{1}]", handler.ToString(), resource_name);
        }

        public SSAssetHandlerNotFoundException(ISSAssetArchiveHandler handler, string resource_name) {
            this.handler = handler;
            this.resource_name = resource_name;
        }
    }

	public class SSAssetHandlerLoadException : SSAssetException {
        public ISSAssetArchiveHandler handler;
        public string resource_name;
        public Exception base_exception;

        public override string ToString() {
            return String.Format("[{0}:{1} threw Exception {3}]",
                handler.ToString(), resource_name, base_exception.ToString());
        }


        public SSAssetHandlerLoadException(ISSAssetArchiveHandler handler, string resource_name, Exception e) {
            this.handler = handler;
            this.resource_name = resource_name;
            this.base_exception = e;
        }
    }
	#endregion

	public class SSAssetItem {
		public readonly ISSAssetArchiveHandler handler;
		public readonly string resourceName;
		public SSAssetItem(ISSAssetArchiveHandler handler, string resourceName) {
			this.handler = handler;
			this.resourceName = resourceName;
		}

		public Stream Open() {
			return this.handler.openResource (this.resourceName);
		}
	}

	#region Core Asset Manager
	public class SSAssetManagerContext {
		SSAssetManager mgr;
		string basepath;

		public SSAssetManagerContext(SSAssetManager mgr, string basepath) {
			this.mgr = mgr;
			this.basepath = basepath;
		}

		public SSAssetItem getAsset(string resource_name) {
			return this.mgr.getAsset(Path.Combine(this.basepath, resource_name));
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

        public SSAssetItem getAsset(string resource_name) {                           
            ISSAssetArchiveHandler[] handlers_arr;

            // make sure we are thread-safe on asset handler registration (though this really sholdn't happen)
            lock (this) { handlers_arr = this.handlers.ToArray(); }

            if (handlers_arr.Length == 0) {
                throw new Exception("SSAssetManager: no handlers available for load");
            }

            foreach (ISSAssetArchiveHandler handler in handlers_arr) {
				if (handler.resourceExists (resource_name)) {
					return new SSAssetItem (handler, resource_name);
				}
            }

            // no asset found
            throw new SSNoSuchAssetException(resource_name, handlers_arr);
        }

        public T GetInstance<T>(SSAssetManagerContext context, string filename) {
            object obj = null;
            string fullPath = context.fullHandlePathForResource(filename);
            var key = new Tuple<string, Type>(fullPath, typeof(T));
            bool found = m_instances.TryGetValue(key, out obj);
            if (found) {
                return (T)obj;
            } else {
                return (T)createInstance(context, filename, typeof(T));
            }
        }

        public static T GetInstance<T>(string context, string filename) {
            var ctx = mgr.getContext(context);
            return mgr.GetInstance<T>(ctx, filename);
        }

        public bool DeleteInstance<T>(SSAssetManagerContext context, string filename) {
            string fullPath = context.fullHandlePathForResource(filename);
            var key = new Tuple<string, Type>(fullPath, typeof(T));
            bool ok = m_instances.Remove(key);
            if (!ok) {
                throw new Exception("SSAssetManager: no such instance");
            }
            return ok;
        }

        private object createInstance(SSAssetManagerContext context, string filename, Type resType) {
            ISSAssetArchiveHandler[] handlersArr;
            // make sure we are thread-safe on asset handler registration (though this really sholdn't happen)
            lock (this) { 
                handlersArr = this.handlers.ToArray(); 
            }

            if (handlersArr.Length == 0) {
                throw new Exception("SSAssetManager: no handlers available for load");
            }

            string fullPath = context.fullHandlePathForResource(filename);
            foreach (ISSAssetArchiveHandler handler in handlersArr) {
                if (handler.resourceExists(fullPath)) {
                    Object newObj = null;

                    if (resType == typeof(SSMesh_wfOBJ)) {
                        // todo: disassociate asset manager classes from mesh classes
                        newObj = new SSMesh_wfOBJ(context, filename);
                    }
                    // todo: more type handlers

                    var key = new Tuple<string, Type>(fullPath, resType);
                    m_instances.Add(key, newObj);
                    return newObj;
                }
            }
            throw new SSNoSuchAssetException(filename, handlersArr);
        }

        public SSAssetManagerContext getContextForResource(string fullpath) {
            string basepath = Path.Combine(fullpath, "../");
            return new SSAssetManagerContext(this, basepath);
        }
        public SSAssetManagerContext getContext(string basepath) {
            return new SSAssetManagerContext(this, basepath);
        }

        // todo: replace with a data structure that tracks use and removes Last Recently Used
        private Dictionary<Tuple<string,Type>, object> m_instances 
            = new Dictionary<Tuple<string,Type>, object>();
    }
	#endregion



	#region AssetArchive Filesystem Implementation
	// --------------- Filesystem Implementation of an Asset Context ------------------- 


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

		public bool resourceExists(string resource_name) {
			string fullpath = Path.Combine(this.basepath, resource_name);
			return File.Exists (fullpath);
		}

		public Stream openResource(string resource_name) {
			string fullpath = Path.Combine(this.basepath, resource_name);
			if (!File.Exists(fullpath)) {
				throw new SSAssetHandlerNotFoundException(this, resource_name);
			} else {
				try {
					return File.Open(fullpath, FileMode.Open, FileAccess.Read, FileShare.Read);
				} catch (Exception e) {
					throw new SSAssetHandlerLoadException(this, resource_name, e);
				}
			}
		}

	}
	#endregion
}
