using System;

namespace JoinBox.BasalMath
{
    public static partial class Constant
    {
        /*
         * 圆周率争议 https://baike.baidu.com/item/%CF%84/2858554
         * 有数学家认为真正的圆周率应为2π,而在cad上面也是2π为一个圆.
         * 不过net5.0上面新增的库是如下代码
         */

        /// <summary>
        /// 360度
        /// </summary>
        public const double Tau = Math.PI * 2.0;
        /// <summary>
        /// 90度
        /// </summary>
        public const double PiHalf = Math.PI * 0.5;

        public const double Pi2 = Math.PI * 2.0;
    }
}
