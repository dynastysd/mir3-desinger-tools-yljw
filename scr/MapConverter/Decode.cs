using MirUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapConverter
{
    //这是c++中的代码，初始化的时候会解密pkg字符串
    internal class Decode
    {
        static string[] pkgs = new string[] 
        {
                                "AUjmftd.qlh",
                                "AUjmft30d.qlh",
                                "AUjmft5d.qlh",
                                "ATnUjmftd.qlh",
                                "AIpvtftd.qlh",
                                "ADmjggtd.qlh",
                                "AEvohfpotd.qlh",
                                "AJoofstd.qlh",
                                "AGvsojuvsftd.qlh",
                                "AXbmmtd.qlh",
                                "ATnPckfdutd.qlh",
                                "ABojnbujpotd.qlh",
                                "APckfdu1d.qlh",
                                "APckfdu2d.qlh",
                                "AUjmftdXppe.qlh",
                                "Aujmft30dxppe.qlh",
                                "AUjmft5dXppe.qlh",
                                "Atnujmftdxppe.qlh",
                                "AIpvtftdXppe.qlh",
                                "ADmjggtdXppe.qlh",
                                "AEvohfpotdXppe.qlh",
                                "AJoofstdXppe.qlh",
                                "AGvsojuvsftdXppe.qlh",
                                "AXbmmtdXppe.qlh",
                                "ATnPckfdutdXppe.qlh",
                                "ABojnbujpotdXppe.qlh",
                                "Apckfdu1dxppe.qlh",
                                "Apckfdu2dxppe.qlh",
                                "AUjmftdTboe.qlh",
                                "Aujmft30dtboe.qlh",
                                "AUjmft5dTboe.qlh",
                                "Atnujmftdtboe.qlh",
                                "AIpvtftdTboe.qlh",
                                "ADmjggtdTboe.qlh",
                                "AEvohfpotdTboe.qlh",
                                "Ajoofstdtboe.qlh",
                                "Agvsojuvsftdtboe.qlh",
                                "AXbmmtdTboe.qlh",
                                "ATnPckfdutdTboe.qlh",
                                "ABojnbujpotdTboe.qlh",
                                "Apckfdu1dtboe.qlh",
                                "Apckfdu2dtboe.qlh",
                                "AUjmftdTopx.qlh",
                                "AUjmft30dTopx.qlh",
                                "AUjmft5dTopx.qlh",
                                "ATnUjmftdTopx.qlh",
                                "AIpvtftdTopx.qlh",
                                "ADmjggtdTopx.qlh",
                                "AEvohfpotdTopx.qlh",
                                "AJoofstdTopx.qlh",
                                "Agvsojuvsftdtopx.qlh",
                                "AXbmmtdTopx.qlh",
                                "ATnPckfdutdTopx.qlh",
                                "ABojnbujpotdTopx.qlh",
                                "Apckfdu1dtopx.qlh",
                                "Apckfdu2dtopx.qlh",
                                "AUjmftdGpsftu.qlh",
                                "AUjmft30dGpsftu.qlh",
                                "AUjmft5dGpsftu.qlh",
                                "Atnujmftdgpsftu.qlh",
                                "AIpvtftdGpsftu.qlh",
                                "Admjggtdgpsftu.qlh",
                                "AEvohfpotdGpsftu.qlh",
                                "Ajoofstdgpsftu.qlh",
                                "Agvsojuvsftdgpsftu.qlh",
                                "Axbmmtdgpsftu.qlh",
                                "ATnPckfdutdGpsftu.qlh",
                                "ABojnbujpotdGpsftu.qlh",
                                "Apckfdu1dgpsftu.qlh",
                                "Apckfdu2dgpsftu.qlh",
                                };

        public static void DecodePkg()
        {
            for (int i = 0; i < pkgs.Length; ++i)
            {
                string b = pkgs[i];
                b = "nbq/" + b.Substring(1);
                Utils.Log( Decry(b, 1));
            }
        }
        public static string Decry(string input, int numB)
        {
            int num = 26 - numB; // 计算移动的位数，实现解密
            char[] result = new char[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (char.IsUpper(c))
                {
                    // 处理大写字母
                    result[i] = (char)(((c - 'A' + num) % 26) + 'A');
                }
                else if (char.IsLower(c))
                {
                    // 处理小写字母
                    result[i] = (char)(((c - 'a' + num) % 26) + 'a');
                }
                else
                {
                    // 非字母字符不需要解密，保持原样
                    result[i] = c;
                }
            }

            return new string(result);
        }

    }
}
