namespace QuestProModule;

public static class FbExpression
{
  public const int Max = 63;
  
  // Below is the eye tracking information
  public const int LeftRot_x = 64;
  public const int LeftRot_y = 65;
  public const int LeftRot_z = 66;
  public const int LeftRot_w = 67;
  public const int LeftPos_x = 68;
  public const int LeftPos_y = 70; // Flipped, need to convert RHS to LHS
  public const int LeftPos_z = 69;
  // public const int 71 is unused
  public const int RightRot_x = 72;
  public const int RightRot_y = 73;
  public const int RightRot_z = 74;
  public const int RightRot_w = 75;
  public const int RightPos_x = 76;
  public const int RightPos_y = 78; // Flipped, need to convert RHS to LHS
  public const int RightPos_z = 77;
}
