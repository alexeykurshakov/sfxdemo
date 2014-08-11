package sfx.test;

import java.io.File;
import java.io.IOException;
import java.nio.file.Paths;
import java.util.*;

public class GameRunnerService
{
	private Dictionary<String, Process> _runningProcesses;
	
	private GameRunnerService()
	{
		_runningProcesses = new Hashtable<String, Process>();		
	}
	
	private static class InstanceHolder {
		public final static GameRunnerService Instance = new GameRunnerService();
	}	
	
	public static String ProcessRootPath;
	
	public static GameRunnerService getInstance()
	{	
		return InstanceHolder.Instance;
	}
	
	public void runInRoom(String roomName)
	{
		if (_runningProcesses.get(roomName) != null)
			return;
		
		String processPath = Paths.get(GameRunnerService.ProcessRootPath)
				.resolve("server.exe").toString();				
		File f = new File(processPath);
		if(!f.exists() || f.isDirectory())
			return;		
				
		try
		{
			// is need to defend by mutex ?
			Process p = Runtime.getRuntime().exec(processPath);		
			_runningProcesses.put(roomName, p);
		} catch (IOException e)
		{
			// TODO Auto-generated catch block
			// trace(e.getMessage());
			e.printStackTrace();
		}
	}
	
	public void stopForRoom(String roomName)
	{		
		if (_runningProcesses.get(roomName) == null)
			return;
		
		_runningProcesses.get(roomName).destroy();
		_runningProcesses.remove(roomName);
	}
		
		
	public void stopAll()
	{		
		for (Enumeration<String> e = this._runningProcesses.keys(); e.hasMoreElements();) 
		{		
			Process p = this._runningProcesses.get(e.nextElement());
			p.destroy();
	    }
		_runningProcesses = new Hashtable<String, Process>();
	}
}
