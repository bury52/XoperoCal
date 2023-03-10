using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using xopCal.Entity;
using xopCal.Hubs;
using xopCal.Model;

namespace xopCal.Services;


public class EventCalService : IEventCalService
{
    private readonly EventDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHubContext<EventHub> _hub;

    
    public EventCalService(EventDbContext context,IMapper mapper, IHubContext<EventHub> hub)
    {
        _context = context;
        _mapper = mapper;
        _hub = hub;

    }

    public async Task StartWatch()
    {
        var we = _context.EventCals.Where(e =>  e.StartEvent >= DateTime.Now.ToUniversalTime()).OrderBy(e => e.StartEvent).ToList();
        if (!we.IsNullOrEmpty()) 
        {
            foreach (var eventCal in we)
            {
                Task.Run(() => Watch(eventCal));
                
            }
        }
          
    }
    
    public async Task StartWatch(EventCal ec)
    {
     
            Console.WriteLine(ec.StartEvent >= DateTime.Now.ToUniversalTime());
            if (ec.StartEvent >= DateTime.Now.ToUniversalTime())
            {
                Task.Run(()=>Watch(ec));
            }
            
    }

   private async Task Watch(EventCal ec)
   {
       var r = ec.StartEvent - DateTime.Now.ToUniversalTime();
       await Task.Delay(r);
       // Console.WriteLine(ev.StartEvent >= DateTime.Now.ToUniversalTime().AddMinutes(-5) && ev.StartEvent <= DateTime.Now.ToUniversalTime().AddMinutes(5));
       // if(ev.StartEvent >= DateTime.Now.ToUniversalTime().AddMinutes(-5) && ev.StartEvent <= DateTime.Now.ToUniversalTime().AddMinutes(5))
           await _hub.Clients.All.SendAsync("watch",ec.Id,ec.Name,ec.OwnerId);
   }
   

    public bool PutSnooze(int id,int userId)
    {
        
        EventCal? e = _context.EventCals.FirstOrDefault(e => e.Id == id);
        if (e is not null && e.OwnerId == userId)
        {
            e.StartEvent = e.StartEvent.AddMinutes(5).ToUniversalTime();
            e.EndEvent = e.EndEvent.AddMinutes(5).ToUniversalTime();
            _context.EventCals.Update(e);
            _context.SaveChanges();
            _hub.Clients.All.SendAsync("newevent");
            StartWatch(e);
            return true;
        }
        return false;
        
    }

    public EventCalDtoOut? GetEventCalById(int? id)
    {
        if (id is null)
        {
            return null;
        }
        return _mapper.Map<EventCalDtoOut>(_context.EventCals.Include(e => e.Owner).Include(e => e.Subscribers).FirstOrDefault(e => e.Id == id));
    }
    
    public int GetStatus(int id,int userId)
    {
        var e = _context.EventCals.Include(e => e.Subscribers).FirstOrDefault(e => e.Id == id);
        if (e != null && e.OwnerId == userId) return 1;
        if (e != null && e.Subscribers.Exists(u => u.Id == userId)) return 2;
        return 3;
    }
    
    public List<EventCalDtoOut> GetAllEventCalByUserId(int userId)
    {
        var le = _context.EventCals.Where(e => e.OwnerId == userId).Include(e => e.Owner).Include(e => e.Subscribers).ToList();
        return _mapper.Map<List<EventCalDtoOut>>(le);
    }
    
    public List<EventCalDtoOut> GetAllEventCalByTime(TimeDto dto)
    {
        dto.StartEvent = dto.StartEvent.ToUniversalTime();
        
      
        
        List<EventCal> le;
        if (dto.EndEvent is null)
        { 
            le = _context.EventCals.Where(e => e.StartEvent <= dto.StartEvent && e.EndEvent >= dto.StartEvent).Include(e => e.Owner).Include(e => e.Subscribers).ToList();
        }
        else
        {
            dto.EndEvent = dto.EndEvent.Value.ToUniversalTime();
            le = _context.EventCals.Where(e => e.StartEvent >= dto.StartEvent && e.EndEvent <= dto.EndEvent).Include(e => e.Owner).Include(e => e.Subscribers).ToList();
        }
        return _mapper.Map<List<EventCalDtoOut>>(le);
    }

    public List<EventCalDtoOut> GetAll()
    {
        return _mapper.Map<List<EventCalDtoOut>>(_context.EventCals.Include(e=>e.Owner).Include(e => e.Subscribers).ToList());
    }

    public List<EventCalDtoOut> GetAllEventCalByName(string name)
    {
        List<EventCal> le = _context.EventCals.Include(e => e.Owner).Where(e => e.Name.Contains(name) || e.Owner.Name.Contains(name)).Include(e => e.Subscribers).ToList();
        return _mapper.Map<List<EventCalDtoOut>>(le);
    }
    
    public bool PostEventCal(EventCalDtoIn dtoIn,int userId)
    {
        var e = new EventCal()
        {
            Name = dtoIn.Name ?? "none",
            Description = dtoIn.Description ?? "none",
            StartEvent = dtoIn.StartEvent ?? DateTime.Now,
            Color = dtoIn.Color ?? "#ffffff",
            OwnerId = userId,
        };
        e.EndEvent = dtoIn.EndEvent ?? e.StartEvent.AddDays(1);
        e.StartEvent = e.StartEvent.ToUniversalTime();
        e.EndEvent = e.EndEvent.ToUniversalTime();
        _context.EventCals.Add(e);
        _context.SaveChanges();
        _hub.Clients.All.SendAsync("newevent");
        StartWatch(e);
        return true;
    }
    
    public bool PutEventCal(EventCalDtoIn dtoIn,int userId)
    {
        EventCal? e = _context.EventCals.FirstOrDefault(e => e.Id == dtoIn.Id);
        if (e is not null && e.OwnerId == userId)
        {
            e.Name = dtoIn.Name ?? e.Name;
            e.Description = dtoIn.Description ?? e.Description;
            e.StartEvent = dtoIn.StartEvent ?? e.StartEvent;
            e.EndEvent = dtoIn.EndEvent ?? e.EndEvent;
            e.Color = dtoIn.Color ?? e.Color;
            e.StartEvent = e.StartEvent.ToUniversalTime();
            e.EndEvent = e.EndEvent.ToUniversalTime();
            _context.EventCals.Update(e);
            _context.SaveChanges();
            _hub.Clients.All.SendAsync("newevent");
            StartWatch(e);
            return true;
        }
        return false;
    }
    
    public bool DeleteEventCal(int id,int userId)
    {
        var e = _context.EventCals.FirstOrDefault(e => e.Id == id);
        if (e is not null && e.OwnerId == userId)
        {
            _context.EventCals.Remove(e);
            _context.SaveChanges();
            _hub.Clients.All.SendAsync("newevent");
            return true;
        }
        return false;
    }

    public bool Subscribe(int id,int userId)
    {
        var e = _context.EventCals.Include(e => e.Subscribers).FirstOrDefault(e => e.Id == id);
        var u = _context.Users.FirstOrDefault(u => u.Id == userId);

        if (e is not null && u is not null && e.OwnerId != userId && !e.Subscribers.Contains(u))
        {
            e.Subscribers.Add(u);
            _context.EventCals.Update(e);
            _context.SaveChanges();
            _hub.Clients.Users(new []{$"{e.OwnerId}",$"{u.Id}"}).SendAsync("Subscribe",e.Name,u.Name);
            return true;
        }
        return false;
        
    }
    
    public bool UnSubscribe(int id,int userId)
    {
        var e = _context.EventCals.Include(e => e.Subscribers).FirstOrDefault(e => e.Id == id);
        var u = _context.Users.FirstOrDefault(u => u.Id == userId);

        if (e is not null && u is not null && e.OwnerId != userId && e.Subscribers.Contains(u))
        {
            e.Subscribers.Remove(u);
            _context.EventCals.Update(e);
            _context.SaveChanges();
            _hub.Clients.Users(new []{$"{e.OwnerId}",$"{u.Id}"}).SendAsync("UnSubscribe",e.Name,u.Name);
            return true;
        }
        return false;
        
    }

}