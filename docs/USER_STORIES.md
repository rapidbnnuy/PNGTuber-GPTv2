As a Twitch Viewer when I send a message to the chat an event will fire from Streamer Bot, that event will then be entered into a queue. A consumer will then pick up the event based on its type and process it. We should check if the viewer is in the IgnoreBotNames list in AppSettings, which is a comma separated list of names. If the viewer is in the list, we should skip processing the event. It should then determine if the message is a command, and enqueue an event in the command processing queue. If the message is not a command, we should then check if the user exists in the user profile service, if it doesn't we create a new user. Once we have a user record we proceed to check for pronouns if they exist in any of the stores, and if not we request them and then write them to the KV store and the DB. Finally, the user message should be saved to the message queue in memory in KV store, as "<Username> / (<Nickname>) (Pronouns) said <Message>." Once this is all completed, it will then mark the event as completed.

As a Twitch Viewer when I type !teach <Knowledge> in chat, it should process the message as a command, and it should allow me save up to 500 characters about myself to the database, finally returning a confirmation message to the chat. If I trigger the command again, it should overwrite my fact. This should be written and overwritten to both the KV store and the DB. Once this is all completed, it will then mark the event as completed.

As a Twitch Viewer when I type !forget in chat, it should process the message as a command, and it should allow me to remove the knowledge from my profile, finally returning a confirmation message to the chat. This should be removed from both the KV store and the DB. Once this is all completed, it will then mark the event as completed.

As as a Twitch Viewer when I type !help in chat, it should process the message as a command, and it should return a confirmation message to the chat with a list of available commands. Once this is all completed, it will then mark the event as completed.

As a Twitch Viewer when I type !version in chat, it should process the message as a command, and it should return a confirmation message to the chat with the version of the bot. Once this is all completed, it will then mark the event as completed.

As a Twitch Viewer when I type !setpronouns, it should process the message as a command, and it should return information to chat as the bot on how to set your pronouns at https://pr.alejo.io/. Once this is all completed, it will then mark the event as completed.

As a Twitch Viewer when I type !sayplay the bot should process the message as a command, and it should say "!play" in chat. Once this is all completed, it will then mark the event as completed. 


