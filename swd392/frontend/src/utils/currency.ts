/**
 * Format a number as Vietnamese Dong (VND) currency
 * @param amount - The amount to format
 * @returns Formatted currency string (e.g., "50,000 VND")
 */
export const formatVND = (amount: number): string => {
  return `${new Intl.NumberFormat('vi-VN').format(amount)} VND`;
};

/**
 * Format a number as VND without the currency symbol
 * @param amount - The amount to format
 * @returns Formatted number string
 */
export const formatVNDNumber = (amount: number): string => {
  return new Intl.NumberFormat('vi-VN').format(amount);
};
