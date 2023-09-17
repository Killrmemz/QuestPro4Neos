using OscCore;
using System;

namespace QuestProModule;

public class FbMessage
{
  private const int NaturalExpressionsCount = 63;
  private const float SranipalNormalizer = 0.75f;
  public readonly float[] Expressions = new float[NaturalExpressionsCount + 8 * 2];

  public void ParseOsc(OscMessageRaw message)
  {
    Array.Clear(Expressions, 0, Expressions.Length);
    int index = 0;
    foreach (var arg in message)
    {
      // this osc library is strange.
      var localArg = arg;
      Expressions[index] = message.ReadFloat(ref localArg);

      index++;
    }

    // Clear the rest if it wasn't long enough for some reason.
    for (; index < Expressions.Length; index++)
    {
      Expressions[index] = 0.0f;
    }

    PrepareUpdate();
  }

  private static bool FloatNear(float f1, float f2) => Math.Abs(f1 - f2) < 0.0001;

  private void PrepareUpdate()
  {
    // Eye Expressions

    double qX = Expressions[FbExpression.LeftRot_x];
    double qY = Expressions[FbExpression.LeftRot_y];
    double qZ = Expressions[FbExpression.LeftRot_z];
    double qW = Expressions[FbExpression.LeftRot_w];

    double yaw = Math.Atan2(2.0 * (qY * qZ + qW * qX), qW * qW - qX * qX - qY * qY + qZ * qZ);
    double pitch = Math.Asin(-2.0 * (qX * qZ - qW * qY));
    // Not needed for eye tracking
    // double roll = Math.Atan2(2.0 * (q_x * q_y + q_w * q_z), q_w * q_w + q_x * q_x - q_y * q_y - q_z * q_z); 

    // From radians
    double pitchL = 180.0 / Math.PI * pitch;
    double yawL = 180.0 / Math.PI * yaw;

    qX = Expressions[FbExpression.RightRot_x];
    qY = Expressions[FbExpression.RightRot_y];
    qZ = Expressions[FbExpression.RightRot_z];
    qW = Expressions[FbExpression.RightRot_w];

    yaw = Math.Atan2(2.0 * (qY * qZ + qW * qX), qW * qW - qX * qX - qY * qY + qZ * qZ);
    pitch = Math.Asin(-2.0 * (qX * qZ - qW * qY));

    // From radians
    double pitchR = 180.0 / Math.PI * pitch;
    double yawR = 180.0 / Math.PI * yaw;

    // Face Expressions

    Expressions[FbExpression.Eyes_Look_Up_L] *= 0.55f;
    Expressions[FbExpression.Eyes_Look_Up_R] *= 0.55f;
    Expressions[FbExpression.Eyes_Look_Down_L] *= 1.5f;
    Expressions[FbExpression.Eyes_Look_Down_R] *= 1.5f;

    Expressions[FbExpression.Eyes_Look_Left_L] *= 0.85f;
    Expressions[FbExpression.Eyes_Look_Right_L] *= 0.85f;
    Expressions[FbExpression.Eyes_Look_Left_R] *= 0.85f;
    Expressions[FbExpression.Eyes_Look_Right_R] *= 0.85f;

    // Hack: turn rots to looks
    // Pitch = 29(left)-- > -29(right)
    // Yaw = -27(down)-- > 27(up)

    if (pitchL > 0)
    {
      Expressions[FbExpression.Eyes_Look_Left_L] = Math.Min(1, (float)(pitchL / 29.0)) * SranipalNormalizer;
      Expressions[FbExpression.Eyes_Look_Right_L] = 0;
    }
    else
    {
      Expressions[FbExpression.Eyes_Look_Left_L] = 0;
      Expressions[FbExpression.Eyes_Look_Right_L] = Math.Min(1, (float)(-pitchL / 29.0)) * SranipalNormalizer;
    }

    if (yawL > 0)
    {
      Expressions[FbExpression.Eyes_Look_Up_L] = Math.Min(1, (float)(yawL / 27.0)) * SranipalNormalizer;
      Expressions[FbExpression.Eyes_Look_Down_L] = 0;
    }
    else
    {
      Expressions[FbExpression.Eyes_Look_Up_L] = 0;
      Expressions[FbExpression.Eyes_Look_Down_L] = Math.Min(1, (float)(-yawL / 27.0)) * SranipalNormalizer;
    }


    if (pitchR > 0)
    {
      Expressions[FbExpression.Eyes_Look_Left_R] = Math.Min(1, (float)(pitchR / 29.0)) * SranipalNormalizer;
      Expressions[FbExpression.Eyes_Look_Right_R] = 0;
    }
    else
    {
      Expressions[FbExpression.Eyes_Look_Left_R] = 0;
      Expressions[FbExpression.Eyes_Look_Right_R] = Math.Min(1, (float)(-pitchR / 29.0)) * SranipalNormalizer;
    }

    if (yawR > 0)
    {
      Expressions[FbExpression.Eyes_Look_Up_R] = Math.Min(1, (float)(yawR / 27.0)) * SranipalNormalizer;
      Expressions[FbExpression.Eyes_Look_Down_R] = 0;
    }
    else
    {
      Expressions[FbExpression.Eyes_Look_Up_R] = 0;
      Expressions[FbExpression.Eyes_Look_Down_R] = Math.Min(1, (float)(-yawR / 27.0)) * SranipalNormalizer;
    }
  }
}
