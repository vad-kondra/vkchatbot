using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VkNet.Utils;

namespace VkChatBot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallbackController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IVkApi _vkApi;

        private static Dictionary<int, Message> _messages = new();
        private static int _start = 0;
        

        public CallbackController(IConfiguration configuration, IVkApi vkApi)
        {
            _configuration = configuration;
            _vkApi = vkApi;
        }
        
        [HttpPost]
        public IActionResult Callback([FromBody] Updates updates)
        {
            // Проверяем, что находится в поле "type" 
            switch (updates.Type)
            {
                // Если это уведомление для подтверждения адреса
                case "confirmation":
                    // Отправляем строку для подтверждения 
                    return Ok(_configuration["Config:Confirmation"]);
                case "message_new":{
                    // Десериализация
                    var msg = Message.FromJson(new VkResponse(updates.Object));
                    
                    _messages.Add(_start, msg);
                    _start++;

                    // Отправим в ответ полученный от пользователя текст
                    var message = new MessagesSendParams
                    {
                        RandomId = new DateTime().Millisecond,
                        PeerId = msg.PeerId.Value,
                        Message = msg.Text
                    };
                    
                    _vkApi.Messages.Send(message);
                    break;
                }
            }
            // Возвращаем "ok" серверу Callback API
            return Ok("ok");
        }
        
        [HttpGet]
        public IActionResult Send()
        {

            var dialogs = _vkApi.Messages.GetConversations(new GetConversationsParams());

            var messages = _vkApi.Messages.GetHistory(new MessagesGetHistoryParams()
            {
                PeerId = dialogs.Items[0].Conversation.Peer.Id
            });

            var result = _messages;

            return Ok(result);
        }
    }
}