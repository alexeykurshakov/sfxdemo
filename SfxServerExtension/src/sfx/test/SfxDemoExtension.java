package sfx.test;

import com.smartfoxserver.v2.core.SFSEventType;
import com.smartfoxserver.v2.extensions.SFSExtension;

import sfx.test.EventHandlers.UserJoinRoomEventHandler;
import sfx.test.EventHandlers.UserLeaveRoomEventHandler;

public class SfxDemoExtension extends SFSExtension
{
	@Override
	public void init() 
	{			
		GameRunnerService.ProcessRootPath = this.getCurrentFolder();
		
		addEventHandler(SFSEventType.USER_JOIN_ROOM, UserJoinRoomEventHandler.class);		
		addEventHandler(SFSEventType.USER_LEAVE_ROOM, UserLeaveRoomEventHandler.class);
		addEventHandler(SFSEventType.USER_DISCONNECT, UserLeaveRoomEventHandler.class);			 		
	}	
}
