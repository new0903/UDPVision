
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MessageProtocolLibrary
{
    public delegate void MessageComplete(byte[] buffer);


    /*
     Я не много не продумал этот класс

    Это форма для одного Сообщения или для сообщений вот в чем беда

    если создовать его то для отправки 1 сообщения
    а если принимать то можно принимать много сообщений разных
    этот нюанс надо продумать по лучше
     
     */
    public class RegisterFiles
    {
        public event MessageComplete RecieveMessaging;

        public string IdMessage;

        public RegisterFiles() { 
            IdMessage = Guid.NewGuid().ToString();
        }

        public Queue<byte[]> PackMessage { get; private set; } = new Queue<byte[]>();

        public List<MessageProtocol> UnpackMessage { get; private set; } = new List<MessageProtocol>();

        private double maxFileSizeSend = 65000;

        //дописать этот метод( сделать разбивку буффера на мелкие кусочки)
        //этот код не идеален можно засрать оперативную память что бы такого не произшло надо брать по кускам
        public void CreateMessage( byte[] buffer)
        {
            int size = buffer.Length;
            var maxSteps = (int)Math.Floor(size / maxFileSizeSend);


            for (int i = 0; i <= maxSteps; i++)
            {
                var startSlice = i * maxFileSizeSend;
                var endSlice = (i + 1) * maxFileSizeSend;
                if (endSlice >= size)
                {
                    endSlice = size;
                }


                int sizeBuffer = (int)(endSlice - startSlice);
                byte[]? bufferMessage = new byte[sizeBuffer];
                var test=buffer.Skip((int)startSlice).Take(sizeBuffer).ToArray();



                MessageProtocol message = new MessageProtocol();
                message.size = size;
                message.MaxStepFile = maxSteps;
                message.CurStepFile = i;
                message.IdMessage = IdMessage;
                message.DispatchTime=DateTime.Now;
                message.FileBytes = test;

                var messageByte = MessageProtocol.PackMessage(message);

                PackMessage.Enqueue(messageByte);
                bufferMessage = null;
            }
        }

        public void CompleteMessage(byte[] buffer)
        {
            var messageUnpack = MessageProtocol.UnpackMessage(buffer);
            UnpackMessage.Add(messageUnpack);

            if (messageUnpack.MaxStepFile== messageUnpack.CurStepFile&& 
                UnpackMessage.Where(up => up.IdMessage == messageUnpack.IdMessage).Count()== messageUnpack.MaxStepFile+1)
            {

                var messageByte = GetBytesForUpload(messageUnpack.IdMessage);
                if (messageByte!=null)
                {

                    RecieveMessaging.Invoke(messageByte);
                    var messagesForDelete = UnpackMessage.Where(up=>up.IdMessage== messageUnpack.IdMessage).ToArray();
                    foreach (var item in messagesForDelete)
                    {
                        UnpackMessage.Remove(item);
                    }
                }
            }
        }

        public byte[] GetBytesForUpload(string idMessage)
        {

            var arrayBase64String = UnpackMessage
            .Where(up=>up.IdMessage==idMessage)
            .OrderBy(ms => ms.CurStepFile)
            .Select(ms => ms.FileBytes)
            .ToArray();


            int countArrayBase64 = arrayBase64String.Length;
            byte[] responseByte = new byte[0];


            for (int i = 0; i < countArrayBase64; i++)
            {

                var test = arrayBase64String[i];
                responseByte = responseByte.Concat(test).ToArray();

            }
            return responseByte;
        }






    }
}
