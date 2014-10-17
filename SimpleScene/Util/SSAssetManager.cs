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
// 	Context ctx = SSAssetManager.mgr.getContext("./drone2/");
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

	#region Core Asset Manager
    public class SSAssetManager {
        
        private static readonly SSAssetManager s_mgr = new SSAssetManager();

        private readonly List<ISSAssetArchiveHandler> m_handlers = new List<ISSAssetArchiveHandler>();
        
        // todo: replace with a data structure that tracks use and removes Last Recently Used
        private Dictionary<Tuple<string, Type>, object> m_instances
            = new Dictionary<Tuple<string, Type>, object>();

        private Dictionary<Type, LoadDelegate> m_loadDelegates = new Dictionary<Type,LoadDelegate>();

        #region Public User Functions and Classes
        public delegate object LoadDelegate(Context ctx, string filename);

        static public void RegisterLoadDelegate<T>(LoadDelegate dlg) {
            s_mgr.registerLoadDelegate<T>(dlg);
        }

        static public T GetInstance<T>(SSAssetManager.Context context, string filename) {
            return s_mgr.getInstance<T>(context, filename);
        }

        static public T GetInstance<T>(string context, string filename) {
            var ctx = new Context(context);
            return s_mgr.getInstance<T>(ctx, filename);
        }

        static public void DeleteInstance<T>(Context context, string filename) {
            s_mgr.deleteInstance<T>(context, filename);
        }

        static public void AddAssetArchive(ISSAssetArchiveHandler handler) {
            s_mgr.addAssetArchive(handler);
        }
        #endregion

        private void registerLoadDelegate<T>(LoadDelegate dlg) {
            Type type = typeof(T);
            lock (this) {
                if (!m_loadDelegates.ContainsKey(type)) {
                    m_loadDelegates.Add(typeof(T), dlg);
                } else {
                    LoadDelegate existing;
                    m_loadDelegates.TryGetValue(type, out existing);
                    if (dlg != existing) {
                        throw new Exception("SSAssetManager: Conflicting registration of a load delegate");
                    }
                }
            }
        }

        private void addAssetArchive(ISSAssetArchiveHandler handler) {
            lock (this) {
                m_handlers.Add(handler);
            }
        }
        
        private Stream openStream(string fullPath) {
            ISSAssetArchiveHandler[] handlersArr;

            lock (this) { handlersArr = this.m_handlers.ToArray(); }

            if (handlersArr.Length == 0) {
                throw new Exception("SSAssetManager: no handlers available for load");
            }

            foreach (ISSAssetArchiveHandler handler in handlersArr) {
                if (handler.resourceExists(fullPath)) {
                    return handler.openResource(fullPath);
                }
            }
            throw new SSNoSuchAssetException(fullPath, handlersArr);
        }

        private T getInstance<T>(Context context, string filename) {
            object obj = null;
            string fullPath = context.fullResourcePath(filename);
            var key = new Tuple<string, Type>(fullPath, typeof(T));
            bool found;
            lock (this) {
                found = m_instances.TryGetValue(key, out obj);
            }
            if (found) {
                return (T)obj;
            } else {
                return (T)createInstance(context, filename, typeof(T));
            }
        }

        private bool deleteInstance<T>(Context context, string filename) {
            string fullPath = context.fullResourcePath(filename);
            var key = new Tuple<string, Type>(fullPath, typeof(T));
            bool ok;
            lock (this) {
                ok = m_instances.Remove(key);
            }
            if (!ok) {
                throw new Exception("SSAssetManager: no such instance");
            }
            return ok;
        }

        private object createInstance(Context context, string filename, Type resType) {
            ISSAssetArchiveHandler[] handlersArr;
            // make sure we are thread-safe on asset handler registration (though this really sholdn't happen)
            lock (this) { 
                handlersArr = this.m_handlers.ToArray(); 
            }

            if (handlersArr.Length == 0) {
                throw new Exception("SSAssetManager: no handlers available for load");
            }

            string fullPath = context.fullResourcePath(filename);
            foreach (ISSAssetArchiveHandler handler in handlersArr) {
                if (handler.resourceExists(fullPath)) {
                    LoadDelegate dlg;
                    bool found;
                    lock (this) {
                        found = m_loadDelegates.TryGetValue(resType, out dlg);
                    }
                    if (!found) {
                        throw new Exception("SSAssetManager: Load delegate not found");
                    }
                    Object newObj = dlg(context, filename);
                    var key = new Tuple<string, Type>(fullPath, resType);
                    lock(this) {
                        m_instances.Add(key, newObj);
                    }
                    return newObj;
                }
            }
            throw new SSNoSuchAssetException(filename, handlersArr);
        }

        public class Context
        {
            private readonly string m_basepath;

            public Context(string basepath) {
                m_basepath = basepath;
            }

            public Stream Open(string filename) {
                string fullPath = fullResourcePath(filename);
                return s_mgr.openStream(fullPath);
            }

            public string fullResourcePath(string filename) {
                return Path.Combine(m_basepath, filename);
            }
        }
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
