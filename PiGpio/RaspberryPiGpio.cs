using Raspberry.IO.Interop;
using Raspberry.Timers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NirDobovizki.PiGpio
{
    public class RaspberryPiGpio
    {
        private const int GPIO_MIN = 0;
        private const int GPIO_MAX = 53;
        private const int GPSET0 = 7 * 4;
        private const int GPSET1 = 8 * 4;
        private const int GPCLR0 = 10 * 4;
        private const int GPCLR1 = 11 * 4;
        private const int GPLEV0 = 13 * 4;
        private const int GPLEV1 = 14 * 4;
        private const ulong GPIO_BASE_OFFSET = 0x00200000;
        private const ulong BLOCK_SIZE = (4 * 1024);
        private const int GPPUD = 37 * 4;
        private const int GPPUDCLK0 = 38 * 4;
        private const int GPPUDCLK1 = 39 * 4;

        enum PullUpDown
        {
            PULL_UNSET = -1,
            PULL_NONE = 0,
            PULL_DOWN = 1,
            PULL_UP = 2,
        }

        enum Function
        {
            FUNC_UNSET = -1,
            FUNC_IP = 0,
            FUNC_OP = 1,
            FUNC_A0 = 4,
            FUNC_A1 = 5,
            FUNC_A2 = 6,
            FUNC_A3 = 7,
            FUNC_A4 = 3,
            FUNC_A5 = 2,
        }


        private IntPtr gpio_base;
        private int _fd;

        public RaspberryPiGpio()
        {
            if ((_fd = UnixFile.OpenFileDescriptor("/dev/gpiomem", UnixFileMode.ReadWrite | UnixFileMode.Synchronized | UnixFileMode.O_CLOSEXEC)) >= 0)
            {
                gpio_base = MemoryMap.Create(IntPtr.Zero, BLOCK_SIZE, MemoryProtection.Read | MemoryProtection.Write, MemoryFlags.Shared, _fd, 0);
            }
            else
            {
                /*if (geteuid())
                {
                    printf("Must be root\n");
                    return 0
                }*/

                var hwbase = get_hwbase();

                if (hwbase == 0)
                    throw new Exception("can't get hwbase");

                if ((_fd = UnixFile.OpenFileDescriptor("/dev/mem", UnixFileMode.ReadWrite | UnixFileMode.Synchronized | UnixFileMode.O_CLOSEXEC)) < 0)
                {
                    throw new Exception("Unable to open /dev/mem");
                }

                gpio_base = MemoryMap.Create(IntPtr.Zero, BLOCK_SIZE, MemoryProtection.Read | MemoryProtection.Write, MemoryFlags.Shared, _fd, GPIO_BASE_OFFSET + hwbase);
            }
        }

        private uint get_hwbase()
        {

            var ranges_file = "/proc/device-tree/soc/ranges";
            byte[] ranges = new byte[8];
            UInt32 ret = 0;

            using (var fd = File.OpenRead(ranges_file))
            {
                var read = fd.Read(ranges, 1, ranges.Length);

                if (read == ranges.Length)
                {
                    ret = (uint)(
                          (ranges[4] << 24) |
                          (ranges[5] << 16) |
                          (ranges[6] << 8) |
                          (ranges[7]));

                    if ((ranges[0] != 0x7e) ||
                        (ranges[1] != 0x00) ||
                        (ranges[2] != 0x00) ||
                        (ranges[3] != 0x00) ||
                        ((ret != 0x20000000) && (ret != 0x3f000000)))
                    {

                        throw new Exception("Unexpected ranges data ");//,
                        /*
                               ranges[0], ranges[1], ranges[2], ranges[3],

                               ranges[4], ranges[5], ranges[6], ranges[7]);
                               */
                    }

                }
            }
            return ret;
        }

        public bool ReadGpio(int gpio)
        {
            if (gpio < GPIO_MIN || gpio > GPIO_MAX) throw new ArgumentOutOfRangeException();

            if (gpio < 32)
            {
                return (((ReadMem(gpio_base + GPLEV0)) >> gpio) & 0x1) != 0;
            }
            else
            {
                gpio = gpio - 32;
                return (((ReadMem(gpio_base + GPLEV1)) >> gpio) & 0x1) != 0;
            }
        }

        private void SetPullMode(int gpio, int type)
        {

            if (gpio < GPIO_MIN || gpio > GPIO_MAX) throw new ArgumentOutOfRangeException();
            if (type < 0 || type > 2) throw new ArgumentOutOfRangeException();


            if (gpio < 32)
            {
                WriteMem(gpio_base + GPPUD, type);
                DelayUs(10);
                WriteMem(gpio_base + GPPUDCLK0, 0x1 << gpio);
                DelayUs(10);
                WriteMem(gpio_base + GPPUD, 0);
                DelayUs(10);
                WriteMem(gpio_base + GPPUDCLK0, 0);
                DelayUs(10);
            }
            else
            {
                gpio -= 32;
                WriteMem(gpio_base + GPPUD, type);
                DelayUs(10);
                WriteMem(gpio_base + GPPUDCLK1, 0x1 << gpio);
                DelayUs(10);
                WriteMem(gpio_base + GPPUD, 0);
                DelayUs(10);
                WriteMem(gpio_base + GPPUDCLK1, 0);
                DelayUs(10);
            }
        }

        private void SetFunction(int gpio, int fsel)
        {
            IntPtr tmp;
            int reg = gpio / 10;
            int sel = gpio % 10;
            int mask;
            if (gpio < GPIO_MIN || gpio > GPIO_MAX) throw new ArgumentOutOfRangeException();
            tmp = gpio_base + reg;
            mask = 0x7 << (3 * sel);
            mask = ~mask;
            tmp = gpio_base + reg;
            WriteMem(tmp, ReadMem(tmp) & mask);
            WriteMem(tmp, ReadMem(tmp) | ((fsel & 0x7) << (3 * sel)));
            //return (int)((*tmp) >> (3 * sel)) & 0x7;
        }

        public void WriteGpio(int gpio, bool value)
        {

            if (gpio < GPIO_MIN || gpio > GPIO_MAX) throw new ArgumentOutOfRangeException();

            if (value)
            {
                if (gpio < 32)
                {
                    WriteMem(gpio_base + GPSET0, 0x1 << gpio);
                }
                else
                {
                    gpio -= 32;
                    WriteMem(gpio_base + GPSET1, 0x1 << gpio);
                }
            }
            else

            {

                if (gpio < 32)
                {
                    WriteMem(gpio_base + GPCLR0, 0x1 << gpio);
                }
                else
                {
                    gpio -= 32;
                    WriteMem(gpio_base + GPCLR1, 0x1 << gpio);
                }
            }
        }

        private int ReadMem(IntPtr address)
        {
            unchecked
            {
                return Marshal.ReadInt32(address);
            }
        }

        private void WriteMem(IntPtr address, int value)
        {
            unchecked
            {
                Marshal.WriteInt32(address, value);
                DelayUs(5);
                Marshal.WriteInt32(address, value);
            }
        }

        private void DelayUs(int us)
        {
            HighResolutionTimer.Sleep(TimeSpanUtility.FromMicroseconds(us));
        }

        public void ConfigureInput(int gpioPinNumber, PullMode pullMode)
        {
            SetFunction(gpioPinNumber, (int)Function.FUNC_IP);
            SetPullMode(gpioPinNumber, (int)(pullMode == PullMode.PullUp ? PullUpDown.PULL_UP : pullMode == PullMode.PullDown ? PullUpDown.PULL_DOWN : PullUpDown.PULL_NONE));
        }

        public void ConfigureOutput(int gpioPinNumber)
        {
            SetFunction(gpioPinNumber, (int)Function.FUNC_OP);
        }
    }
}
