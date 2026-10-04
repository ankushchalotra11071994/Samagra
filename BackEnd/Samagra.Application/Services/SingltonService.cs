using System;

namespace Samagra.Application.Services
{


    sealed class SignltonService
    {
        private static  SignltonService? _instance;
        private static readonly object _syc= new();
        public static SignltonService CreateInstance
        {
            get
            {
                 if(_instance==null)
                {
                    lock(_syc)
                    {
                        if (_instance == null)
                        {
                            _instance= new SignltonService();
                        }
                    }
                }
                return _instance;
            }
             
        }


    
        private SignltonService()
        {

        }

        public void MyMethod(string parameter)
        {

        }





    }
}