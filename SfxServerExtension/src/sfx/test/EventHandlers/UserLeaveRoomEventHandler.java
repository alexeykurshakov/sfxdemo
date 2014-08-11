package sfx.test.EventHandlers;

import java.util.List;

import sfx.test.GameRunnerService;

import com.smartfoxserver.v2.core.ISFSEvent;
import com.smartfoxserver.v2.core.SFSEventParam;
import com.smartfoxserver.v2.entities.Room;
import com.smartfoxserver.v2.entities.User;
import com.smartfoxserver.v2.exceptions.SFSException;
import com.smartfoxserver.v2.extensions.BaseServerEventHandler;

public class UserLeaveRoomEventHandler extends BaseServerEventHandler 
{ 
	private void roomLeaveEvandHandler(Room leaveRoom)
	{	
		List<User> users = leaveRoom.getUserList();			
		int userCount = users.size();		
				
		if (userCount > 1 ||
				(userCount == 1 && !users.get(0).getName().equals("root")))
			return;					
								
		GameRunnerService.getInstance().stopForRoom(leaveRoom.getName());		
	}
	
	@Override
	public void handleServerEvent(ISFSEvent event) throws SFSException 
	{
		User user = (User) event.getParameter(SFSEventParam.USER);	
		if (user.getName().equals("root"))
			return;
		
		Room room = (Room) event.getParameter(SFSEventParam.ROOM);
		if (room != null)
		{		
			this.roomLeaveEvandHandler(room);
			return;				
		}
		
		List<Room> allRooms = user.getZone().getRoomList();		
		for (Room it : allRooms)
		{
			this.roomLeaveEvandHandler(it);
		}						
	}
}