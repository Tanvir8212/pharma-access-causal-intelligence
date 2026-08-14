namespace PharmaAccess.Application.MachineLearning;
public sealed record VerifiedStateMetadata(int StateId,string StateCode,string StateName);
public static class VerifiedStateReference
{
 public static IReadOnlyList<VerifiedStateMetadata> All{get;}=
 [
  new(1,"AL","Alabama"),new(2,"AK","Alaska"),new(4,"AZ","Arizona"),new(5,"AR","Arkansas"),new(6,"CA","California"),
  new(8,"CO","Colorado"),new(9,"CT","Connecticut"),new(10,"DE","Delaware"),new(11,"DC","District of Columbia"),new(12,"FL","Florida"),
  new(13,"GA","Georgia"),new(15,"HI","Hawaii"),new(16,"ID","Idaho"),new(17,"IL","Illinois"),new(18,"IN","Indiana"),
  new(19,"IA","Iowa"),new(20,"KS","Kansas"),new(21,"KY","Kentucky"),new(22,"LA","Louisiana"),new(23,"ME","Maine"),
  new(24,"MD","Maryland"),new(25,"MA","Massachusetts"),new(26,"MI","Michigan"),new(27,"MN","Minnesota"),new(28,"MS","Mississippi"),
  new(29,"MO","Missouri"),new(30,"MT","Montana"),new(31,"NE","Nebraska"),new(32,"NV","Nevada"),new(33,"NH","New Hampshire"),
  new(34,"NJ","New Jersey"),new(35,"NM","New Mexico"),new(36,"NY","New York"),new(37,"NC","North Carolina"),new(38,"ND","North Dakota"),
  new(39,"OH","Ohio"),new(40,"OK","Oklahoma"),new(41,"OR","Oregon"),new(42,"PA","Pennsylvania"),new(44,"RI","Rhode Island"),
  new(45,"SC","South Carolina"),new(46,"SD","South Dakota"),new(47,"TN","Tennessee"),new(48,"TX","Texas"),new(49,"UT","Utah"),
  new(50,"VT","Vermont"),new(51,"VA","Virginia"),new(53,"WA","Washington"),new(54,"WV","West Virginia"),new(55,"WI","Wisconsin"),new(56,"WY","Wyoming")
 ];
 public static VerifiedStateMetadata Get(int id)=>All.SingleOrDefault(x=>x.StateId==id)??throw new InvalidDataException("Historical StateId is absent from the verified jurisdiction reference.");
}
