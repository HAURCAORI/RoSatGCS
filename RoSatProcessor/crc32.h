/*
 * Copyright (c) 2020-2022 EnduroSat AD. All rights reserved.
 *
 * Contents and presentations are protected world-wide.
 * Any kind of using, copying etc. is prohibited without prior permission.
 */
#pragma once

 /**
  * @addtogroup crc
  * @{
  *
  * @file    MACCRC32_Classic_Impl.h
  * @brief   CRC32 Classic Implementation header
  *
  * @}
  */

#include <stddef.h>
#include <stdint.h>

  /**
  * @brief Calculate CRC32 of a sequence of bytes
  * @param pBuff: pointer to the data
  * @param nSize: number of data bytes to calculate for
  * @param nSeed: crc seed
  * @return the calculated crc
  */

#define MSP_CRC32_POLY 0x04C11DB7
uint32_t MACCRC32_Calc32(const char* pBuff, uint32_t nSize, uint32_t nSeed = 0xFFFFFFFFL);
