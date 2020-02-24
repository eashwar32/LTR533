﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;

namespace PICkitS
{
	public class USBRead
	{
		public delegate void DataNotifier();

		public delegate void USBNotifier();

		private const byte TAG_EOD_READ = 128;

		private const byte TAG_FW_VER = 129;

		private const byte TAG_CTRL_BLK_READ = 130;

		private const byte TAG_STATUS_BLOCK = 131;

		private const byte TAG_STATUS_CBUF = 132;

		private const byte TAG_CBUF_1_READ = 133;

		private const byte TAG_CBUF_2_READ = 134;

		private const byte TAG_CBUF_3_READ = 135;

		private const byte TAG_PACKET_ID = 136;

		public const byte TAG_COMMON_DATA_BYTE_V = 16;

		public const byte TAG_COMMON_DATA_BYTES_V = 17;

		public const byte TAG_COMMON_EVENT_MACRO_LOOP_V = 18;

		public const byte TAG_COMMON_EVENT_TIME_V = 19;

		public const byte TAG_COMMON_EVENT_TIME_ROLLOVER_V = 20;

		public const byte TAG_COMMON_EVENT_MACRO_DONE_V = 21;

		public const byte TAG_COMMON_EVENT_MACRO_ROLLOVER_V = 22;

		public const byte TAG_COMMON_EVENT_MACRO_ABORT_V = 23;

		public const byte TAG_COMMON_EVENT_TIMEOUT_AB0_V = 24;

		public const byte TAG_COMMON_EVENT_TIMEOUT_AB1_V = 25;

		public const byte TAG_COMMON_EVENT_STATUS_ERROR_V = 26;

		public const byte TAG_COMMON_EVENT_END_OF_SCRIPT_V = 27;

		public const byte TAG_COMMON_MARKER_V = 28;

		public const byte TAG_I2C_EVENT_START_TX_V = 128;

		public const byte TAG_I2C_EVENT_STOP_TX_V = 129;

		public const byte TAG_I2C_EVENT_RESTART_TX_V = 130;

		public const byte TAG_I2C_EVENT_ACK_TX_V = 131;

		public const byte TAG_I2C_EVENT_NACK_TX_V = 132;

		public const byte TAG_I2C_EVENT_ACK_RX_V = 133;

		public const byte TAG_I2C_EVENT_NACK_RX_V = 134;

		public const byte TAG_I2C_EVENT_BYTE_TX_V = 135;

		public const byte TAG_I2C_EVENT_BYTE_RX_V = 136;

		public const byte TAG_I2C_EVENT_XACT_ERR_V = 137;

		public const byte TAG_I2C_EVENT_STATUS_ERR_V = 138;

		public const byte TAG_I2CS_EVENT_ADDR_RX_V = 192;

		public const byte TAG_I2CS_EVENT_DATA_RX_V = 193;

		public const byte TAG_I2CS_EVENT_DATA_TX_V = 194;

		public const byte TAG_I2CS_EVENT_ACK_RCV_V = 195;

		public const byte TAG_I2CS_EVENT_NACK_RCV_V = 196;

		public const byte TAG_I2CS_EVENT_STOP_V = 198;

		public const byte TAG_I2CS_EVENT_STATUS_ERROR_V = 199;

		public const byte TAG_I2CS_EVENT_DATA_RQ_V = 200;

		public const byte TAG_I2CS_EVENT_REG_READ_V = 201;

		public const byte TAG_I2CS_EVENT_REG_WRITE_V = 202;

		public const byte TAG_I2CS_EVENT_REG_DATA_V = 203;

		public const byte TAG_SPI_EVENT_BYTE_TX_V = 128;

		public const byte TAG_SPI_EVENT_BYTE_RX_V = 129;

		public const byte TAG_SPI_EVENT_STATUS_ERROR_V = 130;

		public const byte TAG_USART_EVENT_BYTE_TX_V = 128;

		public const byte TAG_USART_EVENT_BYTE_RX_V = 129;

		public const byte TAG_USART_EVENT_STATUS_ERROR_V = 130;

		public const byte TAG_USART_EVENT_BREAK_TX_V = 131;

		public const byte TAG_LIN_EVENT_BYTE_RX_V = 128;

		public const byte TAG_LIN_EVENT_BYTE_TX_V = 129;

		public const byte TAG_LIN_EVENT_STATUS_ERR_V = 130;

		public const byte TAG_LIN_EVENT_BREAK_RX_V = 131;

		public const byte TAG_LIN_EVENT_BREAK_TX_V = 132;

		public const byte TAG_LIN_EVENT_AUTO_BAUD_V = 133;

		public const byte TAG_LIN_EVENT_CHECKSUM_ERR_V = 134;

		public const byte TAG_LIN_EVENT_ID_PARITY_ERR_V = 135;

		public const byte TAG_LIN_EVENT_SLAVE_PROFILE_ID_DATA_V = 136;

		public static bool m_ready_to_notify = false;

		public static bool m_OK_to_send_data = true;

		internal static double m_EVENT_TIME_ROLLOVER = 0.0;

		internal static double m_RUNNING_TIME = 0.0;

		private static double m_EVENT_TIME_CONSTANT = 0.000409;

		private static double m_EVENT_TIME_ROLLOVER_CONSTANT = 26.8;

		private static bool m_grab_next_time_marker = false;

		private static bool m_special_status_requested = false;

		private static volatile bool m_we_are_in_read_loop = false;

		private static Thread m_read_thread;

		internal static Mutex m_cbuf2_data_array_mutex = new Mutex(false);

		internal static Mutex m_usb_packet_mutex = new Mutex(false);

		public static EventWaitHandle m_packet_is_ready = null;

		public static AutoResetEvent m_packet_is_copied = null;

		internal static byte[] m_raw_cbuf2_data_array = new byte[20480];

		internal static byte[] m_cbuf2_data_array = new byte[20480];

		internal static volatile uint m_cbuf2_data_array_index;

		private static volatile uint m_required_byte_count;

		private static volatile uint m_raw_cbuf2_data_array_index;

		private static volatile uint m_cbuf2_packet_byte_count;

		private static volatile bool m_process_data;

		public static volatile bool m_read_thread_is_processing_a_USB_packet;

		internal static uint m_cb2_array_tag_index;

		private static bool m_we_need_next_packet_to_continue;

		private static bool m_user_has_created_synchro_objects = false;

		public static event DataNotifier DataAvailable;

		public static event USBNotifier USBDataAvailable;

		/*
		{
			[MethodImpl(32)]
			add
			{
				USBRead.DataAvailable = (USBRead.DataNotifier)Delegate.Combine(USBRead.DataAvailable, value);
			}
			[MethodImpl(32)]
			remove
			{
				USBRead.DataAvailable = (USBRead.DataNotifier)Delegate.Remove(USBRead.DataAvailable, value);
			}
		}

		public static event USBRead.USBNotifier USBDataAvailable
		{
			[MethodImpl(32)]
			add
			{
				USBRead.USBDataAvailable = (USBRead.USBNotifier)Delegate.Combine(USBRead.USBDataAvailable, value);
			}
			[MethodImpl(32)]
			remove
			{
				USBRead.USBDataAvailable = (USBRead.USBNotifier)Delegate.Remove(USBRead.USBDataAvailable, value);
			}
		}
		*/

		public static void Initialize_Read_Objects()
		{
			USBRead.m_we_are_in_read_loop = false;
			Array.Clear(USBRead.m_raw_cbuf2_data_array, 0, USBRead.m_raw_cbuf2_data_array.Length);
			Array.Clear(USBRead.m_cbuf2_data_array, 0, USBRead.m_cbuf2_data_array.Length);
			USBRead.m_cbuf2_data_array_index = 0u;
			USBRead.m_raw_cbuf2_data_array_index = 0u;
			USBRead.m_cb2_array_tag_index = 0u;
			USBRead.m_cbuf2_packet_byte_count = 0u;
			USBRead.m_required_byte_count = 0u;
			USBRead.m_process_data = true;
			USBRead.m_we_need_next_packet_to_continue = false;
		}

		public static void Dispose_Of_Read_Objects()
		{
			USBRead.m_cbuf2_data_array_mutex.Close();
			USBRead.m_usb_packet_mutex.Close();
			USBRead.m_packet_is_ready.Close();
		}

		public static bool Read_Thread_Is_Active()
		{
			return USBRead.m_we_are_in_read_loop;
		}

		public static void Kill_Read_Thread()
		{
			USBRead.m_we_are_in_read_loop = false;
			if (Utilities.g_comm_mode == Utilities.COMM_MODE.MTOUCH2)
			{
				mTouch2.Send_MT2_RD_STATUS_Command();
				return;
			}
			USBWrite.Send_Status_Request();
		}

		public static void DLL_Should_Process_Data()
		{
			USBRead.m_process_data = true;
		}

		public static void DLL_Should_Not_Process_Data()
		{
			USBRead.m_process_data = false;
		}

		public static bool Kick_Off_Read_Thread()
		{
			bool result = true;
			if (!USBRead.m_we_are_in_read_loop)
			{
				USBRead.m_read_thread = new Thread(new ThreadStart(USBRead.Read_USB_Thread));
				USBRead.m_read_thread.IsBackground=true;
				USBRead.m_read_thread.Start();
			}
			else
			{
				result = false;
			}
			return result;
		}

		public static void ByteaArrayToString(byte[] A)
		{
			StringBuilder hex = new StringBuilder(A.Length * 2);
			foreach(byte b in A)
			{
				hex.AppendFormat("{0:x2}",b);
			}
			Console.WriteLine("Received data:");
			Console.WriteLine(hex.ToString());
		}

		public static void Read_USB_Thread()
		{
			int num = 0;
			bool flag = false;
			USBRead.m_we_are_in_read_loop = true;
			int DataLength = 0;
			int ExDataLength = 0;
			byte [] readData;
			while (USBRead.m_we_are_in_read_loop)
			{
				USBRead.m_read_thread_is_processing_a_USB_packet = false;
				USBRead.m_usb_packet_mutex.WaitOne();
				Array.Clear(Utilities.m_flags.read_buffer, 0, Utilities.m_flags.read_buffer.Length);
				
				//flag = Utilities.ReadFile(Utilities.m_flags.HID_read_handle, Utilities.m_flags.read_buffer, (int)Utilities.m_flags.irbl, ref num, 0);

				try{
				/* replaced hid.dll interface to read */
				ExDataLength = (int)Utilities.m_flags.irbl;
				do{
					num = 0;
					Utilities.m_flags.HID_Handle.ReadTimeoutInMillisecs = 0;
					readData = Utilities.m_flags.HID_Handle.Read(ExDataLength);
					DataLength = readData.Length;	
					if( DataLength > 0 )
					{
						flag = true;
						num = DataLength + 1; 
						//Console.WriteLine("Actual Rx data length: " + DataLength.ToString());
					}
						//Thread.Sleep(10);
				}while( num == 0);
				Utilities.m_flags.read_buffer[0] = 0x00;
				for (int i = 0; i < DataLength; i++)
				{
					Utilities.m_flags.read_buffer[i+1] = readData[i];
				}
				}
				catch( Exception ex )
				{
					Console.WriteLine(ex);

				}
				//num = DataLength + 1;
				//Console.WriteLine("appended Rx data length: " + num.ToString());
				//ByteaArrayToString(Utilities.m_flags.read_buffer);
				/***************************************************/
				
				if (Utilities.m_flags.g_need_to_copy_bl_data)
				{
					for (int i = 0; i < Utilities.m_flags.read_buffer.Length; i++)
					{
						Utilities.m_flags.bl_buffer[i] = Utilities.m_flags.read_buffer[i];
					}
					Utilities.m_flags.g_bl_data_arrived_event.Set();
					Utilities.m_flags.g_need_to_copy_bl_data = false;
				}
				USBRead.m_read_thread_is_processing_a_USB_packet = true;
				USBRead.m_usb_packet_mutex.ReleaseMutex();
				if (num != (int)Utilities.m_flags.irbl || !flag)
				{
					Console.WriteLine("read Thread exit:{0},{1}",num,flag);
					break;
				}
				if (!USBRead.m_we_are_in_read_loop)
				{
					return;
				}
				if (USBRead.m_user_has_created_synchro_objects)
				{
					USBRead.m_packet_is_ready.Set();
					USBRead.m_packet_is_copied.WaitOne();
				}
				if (USBDataAvailable != null && USBRead.m_OK_to_send_data)
				{
					USBDataAvailable();
					USBRead.m_packet_is_copied.WaitOne();
				}
				if (USBRead.m_process_data)
				{
					USBRead.m_usb_packet_mutex.WaitOne();
					USBRead.process_this_packet(ref Utilities.m_flags.read_buffer);
					USBRead.m_usb_packet_mutex.ReleaseMutex();
				}
			}
		}

		public static void Create_Single_Sync_object()
		{
			USBRead.m_packet_is_copied = new AutoResetEvent(false);
		}

		public static ushort Get_USB_IRBL()
		{
			return Utilities.m_flags.irbl;
		}

		public static void Create_Synchronization_Object(ref string p_ready_to_copy)
		{
			string text = DateTime.Now.ToLongTimeString();
			string text2 = "PacketReady" + text;
			USBRead.m_packet_is_ready = new EventWaitHandle(false, 0, text2);
			USBRead.m_packet_is_copied = new AutoResetEvent(false);
			p_ready_to_copy = text2;
			USBRead.m_user_has_created_synchro_objects = true;
		}

		public static void Get_USB_Data_Packet(ref byte[] p_data)
		{
			if (Utilities.m_flags.read_buffer[64] == 0)
			{
				Utilities.m_flags.read_buffer[64] = 0;
			}
			USBRead.m_usb_packet_mutex.WaitOne();
			for (int i = 0; i < Utilities.m_flags.read_buffer.Length; i++)
			{
				p_data[i] = Utilities.m_flags.read_buffer[i];
			}
			USBRead.m_usb_packet_mutex.ReleaseMutex();
			USBRead.m_packet_is_copied.Set();
		}

		private static bool process_this_packet(ref byte[] p_packet)
		{
			bool flag = false;
			try
			{
				USBRead.m_usb_packet_mutex.WaitOne();
				byte b = 1;
				bool flag2 = false;
				byte arg_15_0 = p_packet[1];
				while (b < 64)
				{
					switch (p_packet[(int)b])
					{
						case 128:
							flag2 = true;
							break;
						case 129:
							{
								byte arg_60_0 = p_packet[(int)b];
								USBRead.process_this_group(ref p_packet, (int)b);
								b += 3;
								break;
							}
						case 130:
							{
								byte arg_74_0 = p_packet[(int)b];
								USBRead.process_this_group(ref p_packet, (int)b);
								b += 25;
								break;
							}
						case 131:
							{
								byte arg_89_0 = p_packet[(int)b];
								USBRead.process_this_group(ref p_packet, (int)b);
								b += 21;
								break;
							}
						case 132:
							{
								byte arg_9E_0 = p_packet[(int)b];
								USBRead.process_this_group(ref p_packet, (int)b);
								b += 7;
								break;
							}
						case 133:
						case 134:
						case 135:
							{
								byte arg_B2_0 = p_packet[(int)b];
								USBRead.process_this_group(ref p_packet, (int)b);
								b += (byte)(2 + p_packet[(int)(b + 1)]);
								break;
							}
						case 136:
							{
								byte arg_CE_0 = p_packet[(int)b];
								USBRead.process_this_group(ref p_packet, (int)b);
								b += 2;
								break;
							}
						default:
							flag = true;
							break;
					}
					if (flag2)
					{
						break;
					}
					if (flag || b > 66)
					{
						flag = true;
						break;
					}
				}
				USBRead.m_usb_packet_mutex.ReleaseMutex();
				return flag;
				}
			catch(Exception  Ex)
			{
				Console.WriteLine(Ex);
				return flag;
			}
		}

		private static bool process_this_group(ref byte[] p_data, int p_index)
		{
			bool result = true;
			switch (p_data[p_index])
			{
				case 128:
				case 133:
				case 135:
				case 136:
					if (p_data[p_index + 1] != 0)
					{
						USBRead.m_special_status_requested = true;
					}
					break;
				case 129:
					Utilities.m_flags.g_status_packet_mutex.WaitOne();
					Constants.STATUS_PACKET_DATA[3] = p_data[p_index + 1];
					Constants.STATUS_PACKET_DATA[4] = p_data[p_index + 2];
					Utilities.m_flags.g_status_packet_mutex.ReleaseMutex();
					break;
				case 130:
					Utilities.m_flags.g_status_packet_mutex.WaitOne();
					for (int i = 1; i <= 24; i++)
					{
						Constants.STATUS_PACKET_DATA[6 + i] = p_data[p_index + i];
					}
					Utilities.m_flags.g_status_packet_mutex.ReleaseMutex();
					break;
				case 131:
					Utilities.m_flags.g_status_packet_mutex.WaitOne();
					for (int i = 1; i <= 20; i++)
					{
						Constants.STATUS_PACKET_DATA[31 + i] = p_data[p_index + i];
					}
					Utilities.m_flags.g_status_packet_mutex.ReleaseMutex();
					Utilities.Set_Comm_Mode(p_data[38], p_data[23]);
					break;
				case 132:
					Utilities.m_flags.g_status_packet_mutex.WaitOne();
					for (int i = 1; i <= 6; i++)
					{
						Constants.STATUS_PACKET_DATA[52 + i] = p_data[p_index + i];
					}
					Utilities.m_flags.g_status_packet_mutex.ReleaseMutex();
					Utilities.m_flags.g_status_packet_data_update_event.Set();
					if (USBRead.m_special_status_requested)
					{
						USBRead.m_special_status_requested = false;
						Utilities.m_flags.g_special_status_request_event.Set();
					}
					break;
				case 134:
					USBRead.process_cbuf2_data(ref p_data, p_index);
					break;
				default:
					result = false;
					break;
			}
			return result;
		}

		private static void process_cbuf2_data(ref byte[] p_data, int p_index)
		{
			USBRead.m_cbuf2_packet_byte_count = (uint)p_data[p_index + 1];
			if (USBRead.m_cbuf2_packet_byte_count == 0u)
			{
				return;
			}
			if (!USBRead.m_we_need_next_packet_to_continue)
			{
				USBRead.Clear_Raw_Data_Array();
				if (Utilities.g_comm_mode == Utilities.COMM_MODE.LIN)
				{
					USBRead.Clear_Data_Array(0u);
				}
			}
			int num = p_index + 2;
			while ((long)num < (long)(p_index + 2) + (long)((ulong)USBRead.m_cbuf2_packet_byte_count) && (ulong)USBRead.m_raw_cbuf2_data_array_index < (ulong)((long)USBRead.m_raw_cbuf2_data_array.Length))
			{
				USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index++))] = p_data[num];
				num++;
			}
			USBRead.process_m_cbuf2_array_data();
		}

		private static bool process_m_cbuf2_array_data()
		{
			bool result = false;
			bool flag = false;
			USBRead.m_we_need_next_packet_to_continue = false;
			while (USBRead.m_cb2_array_tag_index < USBRead.m_raw_cbuf2_data_array_index && !flag && !USBRead.m_we_need_next_packet_to_continue)
			{
				if ((USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)] & 16) == 16)
				{
					USBRead.process_common_data(ref flag);
				}
				else if ((USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)] & 128) != 128 && Utilities.g_comm_mode != Utilities.COMM_MODE.MTOUCH2)
				{
					flag = true;
				}
				else
				{
					switch (Utilities.g_comm_mode)
					{
						case Utilities.COMM_MODE.I2C_M:
							USBRead.process_i2c_data(ref flag);
							continue;
						case Utilities.COMM_MODE.SPI_M:
						case Utilities.COMM_MODE.SPI_S:
						case Utilities.COMM_MODE.UWIRE:
							USBRead.process_spi_data(ref flag);
							continue;
						case Utilities.COMM_MODE.USART_A:
						case Utilities.COMM_MODE.USART_SM:
						case Utilities.COMM_MODE.USART_SS:
							USBRead.process_usart_data(ref flag);
							continue;
						case Utilities.COMM_MODE.I2C_S:
							USBRead.process_i2c_slave_data(ref flag);
							continue;
						case Utilities.COMM_MODE.LIN:
							USBRead.process_lin_data(ref flag);
							continue;
						case Utilities.COMM_MODE.MTOUCH2:
							USBRead.process_mtouch2_data(ref flag);
							continue;
					}
					flag = true;
				}
			}
			if (USBRead.DataAvailable != null && USBRead.m_ready_to_notify && USBRead.m_OK_to_send_data)
			{
				USBRead.DataAvailable();
				USBRead.m_ready_to_notify = false;
			}
			return result;
		}

		private static void process_mtouch2_data(ref bool p_error)
		{
			switch (USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)])
			{
				case 65:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 16u)
					{
						mTouch2.m_sensor_status_mutex.WaitOne();
						mTouch2.m_data_status.comm_fw_ver = (ushort)((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 2u))] + ((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 3u))] << 8));
						mTouch2.m_data_status.touch_fw_ver = (ushort)((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 4u))] + ((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 5u))] << 8));
						mTouch2.m_data_status.hardware_id = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 6u))];
						mTouch2.m_data_status.max_num_sensors = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 7u))];
						mTouch2.m_data_status.broadcast_group_id = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 8u))];
						mTouch2.m_data_status.broadcast_enable_flags.trip = ((USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 9u))] & 1) > 0);
						mTouch2.m_data_status.broadcast_enable_flags.guardband = ((USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 9u))] & 2) > 0);
						mTouch2.m_data_status.broadcast_enable_flags.raw = ((USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 9u))] & 4) > 0);
						mTouch2.m_data_status.broadcast_enable_flags.avg = ((USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 9u))] & 8) > 0);
						mTouch2.m_data_status.broadcast_enable_flags.detect_flags = ((USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 9u))] & 16) > 0);
						mTouch2.m_data_status.broadcast_enable_flags.status = ((USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 9u))] & 128) > 0);
						mTouch2.m_data_status.time_interval = (ushort)((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 10u))] + ((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_raw_cbuf2_data_array_index + 11u))] << 8));
						mTouch2.m_sensor_status_mutex.ReleaseMutex();
						mTouch2.m_status_data_is_ready.Set();
						USBRead.m_cb2_array_tag_index += 16u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 66:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 7u)
					{
						int num = 0;
						mTouch2.m_sensor_data_mutex.WaitOne();
						for (uint num2 = USBRead.m_cb2_array_tag_index + 2u; num2 < USBRead.m_cb2_array_tag_index + 2u + 5u; num2 += 1u)
						{
							mTouch2.m_detect_values[num++] = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)num2)];
						}
						mTouch2.m_sensor_data_mutex.ReleaseMutex();
						mTouch2.m_detect_data_is_ready.Set();
						USBRead.m_cb2_array_tag_index += 7u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 67:
					if ((ulong)(USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index) >= (ulong)((long)(USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 2u))] + 2)))
					{
						for (int i = 0; i < (int)(USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 2u))] + 1); i++)
						{
							mTouch2.m_user_sensor_values[i] = USBRead.m_raw_cbuf2_data_array[(int)(checked((IntPtr)(unchecked((ulong)(USBRead.m_cb2_array_tag_index + 2u) + (ulong)((long)i)))))];
						}
						mTouch2.m_user_sensor_values_are_ready.Set();
						USBRead.m_cb2_array_tag_index += (uint)(USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 2u))] + 3);
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 68:
				case 69:
				case 70:
				case 71:
				case 72:
				case 73:
					if ((ulong)(USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index) >= (ulong)((long)(USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 2u))] + 3)))
					{
						int num3 = 0;
						int num4 = (int)(USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 2u))] / 2);
						mTouch2.m_current_sensor_id = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))];
						mTouch2.m_num_current_sensors = (byte)num4;
						switch (USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)])
						{
							case 68:
								{
									mTouch2.m_sensor_data_mutex.WaitOne();
									uint num5 = USBRead.m_cb2_array_tag_index + 3u;
									while ((ulong)num5 < (ulong)(USBRead.m_cb2_array_tag_index + 3u) + (ulong)((long)(num4 * 2)))
									{
										mTouch2.m_trp_values[num3++] = (ushort)((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)num5)] + ((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(num5 + 1u))] << 8));
										num5 += 2u;
									}
									mTouch2.m_sensor_data_mutex.ReleaseMutex();
									mTouch2.m_trip_data_is_ready.Set();
									break;
								}
							case 69:
								{
									mTouch2.m_sensor_data_mutex.WaitOne();
									uint num6 = USBRead.m_cb2_array_tag_index + 3u;
									while ((ulong)num6 < (ulong)(USBRead.m_cb2_array_tag_index + 3u) + (ulong)((long)(num4 * 2)))
									{
										mTouch2.m_gdb_values[num3++] = (ushort)((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)num6)] + ((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(num6 + 1u))] << 8));
										num6 += 2u;
									}
									mTouch2.m_sensor_data_mutex.ReleaseMutex();
									mTouch2.m_gdb_data_is_ready.Set();
									break;
								}
							case 70:
								{
									mTouch2.m_sensor_data_mutex.WaitOne();
									uint num7 = USBRead.m_cb2_array_tag_index + 3u;
									while ((ulong)num7 < (ulong)(USBRead.m_cb2_array_tag_index + 3u) + (ulong)((long)(num4 * 2)))
									{
										mTouch2.m_raw_values[num3++] = (ushort)((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)num7)] + ((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(num7 + 1u))] << 8));
										num7 += 2u;
									}
									mTouch2.m_sensor_data_mutex.ReleaseMutex();
									break;
								}
							case 71:
								{
									mTouch2.m_sensor_data_mutex.WaitOne();
									uint num8 = USBRead.m_cb2_array_tag_index + 3u;
									while ((ulong)num8 < (ulong)(USBRead.m_cb2_array_tag_index + 3u) + (ulong)((long)(num4 * 2)))
									{
										mTouch2.m_avg_values[num3++] = (ushort)((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)num8)] + ((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(num8 + 1u))] << 8));
										num8 += 2u;
									}
									mTouch2.m_sensor_data_mutex.ReleaseMutex();
									break;
								}
							case 72:
								{
									mTouch2.m_sensor_data_mutex.WaitOne();
									uint num9 = USBRead.m_cb2_array_tag_index + 3u;
									while ((ulong)num9 < (ulong)(USBRead.m_cb2_array_tag_index + 3u) + (ulong)((long)(num4 * 2)))
									{
										mTouch2.m_au1_values[num3++] = (ushort)((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)num9)] + ((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(num9 + 1u))] << 8));
										num9 += 2u;
									}
									mTouch2.m_sensor_data_mutex.ReleaseMutex();
									break;
								}
							case 73:
								{
									mTouch2.m_sensor_data_mutex.WaitOne();
									uint num10 = USBRead.m_cb2_array_tag_index + 3u;
									while ((ulong)num10 < (ulong)(USBRead.m_cb2_array_tag_index + 3u) + (ulong)((long)(num4 * 2)))
									{
										mTouch2.m_au2_values[num3++] = (ushort)((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)num10)] + ((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(num10 + 1u))] << 8));
										num10 += 2u;
									}
									mTouch2.m_sensor_data_mutex.ReleaseMutex();
									break;
								}
						}
						USBRead.m_cb2_array_tag_index += (uint)(USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 2u))] + 3);
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				default:
					return;
			}
		}

		private static void process_lin_data(ref bool p_error)
		{
			switch (USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)])
			{
				case 128:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 2u)
					{
						USBRead.m_cb2_array_tag_index += 2u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 129:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 2u)
					{
						USBRead.m_cb2_array_tag_index += 2u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 130:
					LIN.m_working_frame.BuildState.we_had_a_status_error = true;
					if (!LIN.m_working_frame.BuildState.we_timed_out)
					{
						LIN.finalize_working_frame();
						Device.Clear_Status_Errors();
					}
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 2u)
					{
						USBRead.m_cb2_array_tag_index += 2u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 131:
					if (!LIN.m_working_frame.BuildState.we_timed_out)
					{
						if (LIN.m_working_frame.BuildState.we_are_building_a_frame)
						{
							LIN.m_working_frame.BuildState.next_frame_header_received = true;
							LIN.finalize_working_frame();
						}
						LIN.m_working_frame.BuildState.we_are_building_a_frame = true;
						USBRead.m_grab_next_time_marker = true;
						LIN.reset_LIN_timeout();
					}
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 132:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 133:
					if (LIN.m_working_frame.BuildState.we_are_building_a_frame)
					{
						LIN.m_working_frame.FrameInfo.baud = (ushort)((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))] + ((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 2u))] << 8));
						if (LIN.m_use_baud_rate_timeout)
						{
							LIN.calculate_new_baud_dependent_onreceive_timeout((int)LIN.m_working_frame.FrameInfo.baud);
						}
					}
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 3u)
					{
						USBRead.m_cb2_array_tag_index += 3u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 134:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 2u)
					{
						USBRead.m_cb2_array_tag_index += 2u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 135:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 2u)
					{
						USBRead.m_cb2_array_tag_index += 2u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 136:
					LIN.m_slave_profile_id.FrameID = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))];
					LIN.m_slave_profile_id.ByteCount = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 2u))];
					for (int i = 0; i < (int)LIN.m_slave_profile_id.ByteCount; i++)
					{
						LIN.m_slave_profile_id.Data[i] = USBRead.m_raw_cbuf2_data_array[(int)(checked((IntPtr)(unchecked((ulong)(USBRead.m_cb2_array_tag_index + 3u) + (ulong)((long)i)))))];
					}
					USBRead.m_cb2_array_tag_index += (uint)(USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 2u))] + 3);
					LIN.m_slave_profile_id_read.Set();
					return;
				default:
					p_error = true;
					return;
			}
		}

		private static void process_i2c_data(ref bool p_error)
		{
			switch (USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)])
			{
				case 128:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 129:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 130:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 131:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 132:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 133:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 134:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 135:
				case 136:
				case 137:
				case 138:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 2u)
					{
						USBRead.m_cb2_array_tag_index += 2u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				default:
					p_error = true;
					return;
			}
		}

		private static void process_i2c_slave_data(ref bool p_error)
		{
			I2CS.m_slave_address_was_just_set = false;
			I2CS.m_master_is_waiting_for_data = false;
			I2CS.m_stop_command_issued = false;
			switch (USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)])
			{
				case 192:
				case 193:
				case 194:
				case 199:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 2u)
					{
						byte b = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)];
						switch (b)
						{
							case 192:
								I2CS.m_previous_slave_addr_received = I2CS.m_last_slave_addr_received;
								I2CS.m_last_slave_addr_received = (ushort)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))];
								I2CS.m_slave_address_was_just_set = true;
								I2CS.issue_event();
								break;
							case 193:
								USBRead.add_data_to_cbuf2_data_array(ref USBRead.m_raw_cbuf2_data_array, USBRead.m_cb2_array_tag_index + 1u, 1);
								break;
							case 194:
								break;
							default:
								if (b == 199)
								{
									I2CS.issue_error();
								}
								break;
						}
						USBRead.m_cb2_array_tag_index += 2u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 195:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 196:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 198:
					USBRead.m_cb2_array_tag_index += 1u;
					I2CS.m_stop_command_issued = true;
					I2CS.issue_event();
					I2CS.reset_buffers();
					return;
				case 200:
					USBRead.m_cb2_array_tag_index += 1u;
					I2CS.m_master_is_waiting_for_data = true;
					I2CS.issue_event();
					return;
				case 201:
				case 202:
					if ((m_raw_cbuf2_data_array_index - m_cb2_array_tag_index) < 3)
					{
						m_we_need_next_packet_to_continue = true;
						return;
					}
					/*
					switch (m_raw_cbuf2_data_array[m_cb2_array_tag_index])
					{
					}
					*/
					m_cb2_array_tag_index += 3;
					return;
				case 203:
					USBRead.m_cb2_array_tag_index += (uint)(USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 3u))] + 4);
					return;
			}
			p_error = true;
		}

		private static void process_usart_data(ref bool p_error)
		{
			switch (USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)])
			{
				case 128:
				case 129:
				case 130:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 2u)
					{
						USBRead.m_cb2_array_tag_index += 2u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 131:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				default:
					p_error = true;
					return;
			}
		}

		private static void process_spi_data(ref bool p_error)
		{
			switch (USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)])
			{
				case 128:
				case 129:
				case 130:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 2u)
					{
						USBRead.m_cb2_array_tag_index += 2u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				default:
					p_error = true;
					return;
			}
		}

		private static void process_common_data(ref bool p_error)
		{
			switch (USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)])
			{
				case 16:
				case 18:
				case 26:
				case 28:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 2u)
					{
						byte b = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cb2_array_tag_index)];
						switch (b)
						{
							case 16:
								USBRead.add_data_to_cbuf2_data_array(ref USBRead.m_raw_cbuf2_data_array, USBRead.m_cb2_array_tag_index + 1u, 1);
								if (Utilities.g_comm_mode == Utilities.COMM_MODE.LIN && !LIN.m_working_frame.BuildState.we_timed_out && LIN.m_working_frame.BuildState.we_are_building_a_frame)
								{
									if (!LIN.m_working_frame.BuildState.we_have_an_id)
									{
										LIN.m_working_frame.FrameInfo.FrameID = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))];
										LIN.m_working_frame.BuildState.we_have_an_id = true;
									}
									else
									{
										if (LIN.m_working_frame.FrameInfo.bytecount < 9)
										{
											byte[] arg_184_0 = LIN.m_working_frame.FrameInfo.FrameData;
											byte bytecount;
											LIN.m_working_frame.FrameInfo.bytecount = (byte)((bytecount = LIN.m_working_frame.FrameInfo.bytecount) + 1);
											arg_184_0[(int)bytecount] = USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))];
										}
										if (LIN.m_working_frame.FrameInfo.bytecount == 9)
										{
											LIN.finalize_working_frame();
										}
									}
								}
								break;
							case 17:
							case 18:
								break;
							default:
								switch (b)
								{
									case 28:
										if (USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))] == 119)
										{
											Utilities.m_flags.g_PKSA_has_completed_script.Set();
										}
										break;
								}
								break;
						}
						USBRead.m_cb2_array_tag_index += 2u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 17:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index - 2u >= (uint)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))])
					{
						USBRead.add_data_to_cbuf2_data_array(ref USBRead.m_raw_cbuf2_data_array, USBRead.m_cb2_array_tag_index + 2u, (int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))]);
						USBRead.m_cb2_array_tag_index += (uint)(2 + USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))]);
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 19:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 3u)
					{
						if (Utilities.g_comm_mode == Utilities.COMM_MODE.LIN && LIN.m_working_frame.BuildState.we_are_building_a_frame && USBRead.m_grab_next_time_marker)
						{
							double num = (double)(((int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 2u))] << 8) + (int)USBRead.m_raw_cbuf2_data_array[(int)((UIntPtr)(USBRead.m_cb2_array_tag_index + 1u))]) * USBRead.m_EVENT_TIME_CONSTANT;
							USBRead.m_RUNNING_TIME = USBRead.m_EVENT_TIME_ROLLOVER + num;
							LIN.m_working_frame.FrameInfo.time = USBRead.m_RUNNING_TIME;
							USBRead.m_grab_next_time_marker = false;
						}
						USBRead.m_cb2_array_tag_index += 3u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 20:
					USBRead.m_EVENT_TIME_ROLLOVER += USBRead.m_EVENT_TIME_ROLLOVER_CONSTANT;
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 21:
					if (USBRead.m_raw_cbuf2_data_array_index - USBRead.m_cb2_array_tag_index >= 3u)
					{
						USBRead.m_cb2_array_tag_index += 3u;
						return;
					}
					USBRead.m_we_need_next_packet_to_continue = true;
					return;
				case 22:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 23:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 24:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 25:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				case 27:
					USBRead.m_cb2_array_tag_index += 1u;
					return;
				default:
					p_error = true;
					return;
			}
		}

		private static void add_data_to_cbuf2_data_array(ref byte[] p_source, uint p_index, int p_num_bytes)
		{
			int num = 0;
			USBRead.m_cbuf2_data_array_mutex.WaitOne();
			while ((ulong)USBRead.m_cbuf2_data_array_index < (ulong)((long)USBRead.m_cbuf2_data_array.Length) && num < p_num_bytes)
			{
				USBRead.m_cbuf2_data_array[(int)((UIntPtr)USBRead.m_cbuf2_data_array_index)] = p_source[(int)(checked((IntPtr)(unchecked((ulong)p_index + (ulong)((long)num)))))];
				num++;
				USBRead.m_cbuf2_data_array_index += 1u;
			}
			USBRead.m_cbuf2_data_array_mutex.ReleaseMutex();
			if (USBRead.m_cbuf2_data_array_index >= USBRead.m_required_byte_count && USBRead.m_required_byte_count != 0u)
			{
				Utilities.m_flags.g_data_arrived_event.Set();
			}
			USBRead.m_ready_to_notify = true;
		}

		public static uint Retrieve_Data_Byte_Count()
		{
			return USBRead.m_cbuf2_data_array_index;
		}

		public static uint Retrieve_Raw_Data_Byte_Count()
		{
			return USBRead.m_raw_cbuf2_data_array_index;
		}

		public static void DataAvailable_Should_Fire(bool p_value)
		{
			USBRead.m_OK_to_send_data = p_value;
		}

		public static void Clear_Data_Array(uint p_required_byte_count)
		{
			USBRead.m_cbuf2_data_array_mutex.WaitOne();
			Array.Clear(USBRead.m_cbuf2_data_array, 0, USBRead.m_cbuf2_data_array.Length);
			USBRead.m_cbuf2_data_array_index = 0u;
			USBRead.m_required_byte_count = p_required_byte_count;
			USBRead.m_cbuf2_data_array_mutex.ReleaseMutex();
			Utilities.m_flags.g_data_arrived_event.Reset();
		}

		public static void Clear_Raw_Data_Array()
		{
			Array.Clear(USBRead.m_raw_cbuf2_data_array, 0, USBRead.m_raw_cbuf2_data_array.Length);
			USBRead.m_raw_cbuf2_data_array_index = 0u;
			USBRead.m_cb2_array_tag_index = 0u;
		}

		public static bool Retrieve_Data(ref byte[] p_data_array, uint p_num_bytes)
		{
			if (p_num_bytes > USBRead.m_cbuf2_data_array_index)
			{
				return false;
			}
			USBRead.m_cbuf2_data_array_mutex.WaitOne();
			int num = 0;
			while ((long)num < (long)((ulong)p_num_bytes))
			{
				p_data_array[num] = USBRead.m_cbuf2_data_array[num];
				num++;
			}
			int num2 = 0;
			while ((long)num2 < (long)((ulong)(USBRead.m_cbuf2_data_array_index - p_num_bytes)))
			{
				USBRead.m_cbuf2_data_array[num2] = USBRead.m_cbuf2_data_array[(int)(checked((IntPtr)(unchecked((ulong)p_num_bytes + (ulong)((long)num2)))))];
				num2++;
			}
			USBRead.m_cbuf2_data_array_index -= p_num_bytes;
			Array.Clear(USBRead.m_cbuf2_data_array, (int)USBRead.m_cbuf2_data_array_index, USBRead.m_cbuf2_data_array.Length - (int)USBRead.m_cbuf2_data_array_index);
			USBRead.m_cbuf2_data_array_mutex.ReleaseMutex();
			USBRead.m_required_byte_count = 0u;
			return true;
		}

		public static bool Retrieve_Raw_Data(ref byte[] p_data_array, uint p_num_bytes)
		{
			if (p_num_bytes > USBRead.m_raw_cbuf2_data_array_index)
			{
				return false;
			}
			int num = 0;
			while ((long)num < (long)((ulong)p_num_bytes))
			{
				p_data_array[num] = USBRead.m_raw_cbuf2_data_array[num];
				num++;
			}
			int num2 = 0;
			while ((long)num2 < (long)((ulong)(USBRead.m_raw_cbuf2_data_array_index - p_num_bytes)))
			{
				USBRead.m_raw_cbuf2_data_array[num2] = USBRead.m_raw_cbuf2_data_array[(int)(checked((IntPtr)(unchecked((ulong)p_num_bytes + (ulong)((long)num2)))))];
				num2++;
			}
			USBRead.m_raw_cbuf2_data_array_index -= p_num_bytes;
			if (USBRead.m_cb2_array_tag_index < p_num_bytes)
			{
				USBRead.m_cb2_array_tag_index = 0u;
			}
			else
			{
				USBRead.m_cb2_array_tag_index -= p_num_bytes;
			}
			Array.Clear(USBRead.m_raw_cbuf2_data_array, (int)USBRead.m_raw_cbuf2_data_array_index, USBRead.m_raw_cbuf2_data_array.Length - (int)USBRead.m_raw_cbuf2_data_array_index);
			return true;
		}

		public static bool reset_timer_params()
		{
			USBRead.m_EVENT_TIME_ROLLOVER = 0.0;
			USBRead.m_RUNNING_TIME = 0.0;
			return USBWrite.Send_Event_Timer_Reset_Cmd();
		}
	}
}