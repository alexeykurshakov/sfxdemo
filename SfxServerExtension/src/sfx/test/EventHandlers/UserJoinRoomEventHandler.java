package sfx.test.EventHandlers;

import sfx.test.GameRunnerService;

import com.smartfoxserver.v2.core.ISFSEvent;
import com.smartfoxserver.v2.core.SFSEventParam;
import com.smartfoxserver.v2.entities.Room;
import com.smartfoxserver.v2.entities.User;
import com.smartfoxserver.v2.exceptions.SFSException;
import com.smartfoxserver.v2.extensions.BaseServerEventHandler;

public class UserJoinRoomEventHandler extends BaseServerEventHandler 
{ 
	@Override
	public void handleServerEvent(ISFSEvent event) throws SFSException 
	{		
		User user = (User) event.getParameter(SFSEventParam.USER);		
		if (user.getName().equals("root"))
			return;
		
		Room room = user.getLastJoinedRoom();
		GameRunnerService.getInstance().runInRoom(room.getName());			
	}
}