#pragma once
/*
 * Copyright (c) 2020-2022 EnduroSat AD. All rights reserved.
 *
 * Contents and presentations are protected world-wide.
 * Any kind of using, copying etc. is prohibited without prior permission.
 */
#ifndef CRC16_CCITT_FALSE_H_
#define CRC16_CCITT_FALSE_H_

 /**
  * @addtogroup crc
  * @{
  *
  * @file    crc16-ccitt.h
  * @brief   CRC16 CCITT declarations
  *
  * @}
  */

#include <stdint.h>

  /**
  * @brief Calculate CRC16 of a sequence of bytes from the CCITT table using an initial seed
  * 		 value of 0xFFFF
  * @param pDataP: pointer to the data
  * @param iLenP: number of data bytes to calculate for
  * @return the calculated crc
  */
uint16_t crc16_ccitt_false_table(uint8_t const* pDataP, unsigned int iLenP);

/**
* @brief Calculate CRC16 of a sequence of bytes from the CCITT table
* @param pDataP: pointer to the data
* @param iLenP: number of data bytes to calculate for
* @param seed: initial seed value
* @return the calculated crc
*/
uint16_t crc16_ccitt_false_table_ex(uint8_t const* pDataP, uint32_t iLenP, uint16_t seed);

/**
* @brief Calculate CRC16 of a sequence of bytes using the CCITT algorithm
* @param pDataP: pointer to the data
* @param iLenP: number of data bytes to calculate for
* @return the calculated crc
*/
uint16_t crc16_ccitt_false_calc(uint8_t const* pDataP, unsigned int iLenP);

uint16_t crc16_ccitt_false_calc(char const* pDataP, unsigned int iLenP);
/**
* @brief Calculate CRC16 of a sequence of bytes
* @param fp_pu8Data: pointer to the data
* @param fp_u16Count: number of data bytes to calculate for
*
* @return the calculated crc
*/
uint16_t CRC16_8_false(uint8_t* fp_pu8Data, int fp_u16Count);


#endif /* CRC16_CCITT_H_ */
