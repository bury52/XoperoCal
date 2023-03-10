using xopCal.Entity;

namespace xopCal.Model;

public class UserDtoOut
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public List<EventCalDtoOut2> EventCals { get; set; }
    public List<EventCalDtoOut2> SubscribeEventCals { get; set; }
}