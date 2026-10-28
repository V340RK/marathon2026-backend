export function formatTime(timeStr: string): string {
  const date = new Date(timeStr);
  const hour = date.getHours().toString().padStart(2, '0');
  const minute = date.getMinutes().toString().padStart(2, '0');
  return `${hour}:${minute}`;
}

export function formatDate(dateStr: string): string {
  const date = new Date(dateStr);
  return date.toLocaleDateString('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  });
}

export function revertFormatTime(timeStr: string, dateStr: string): string {
  const [time, period = ''] = timeStr.trim().split(' ');
  const [parsedHour, minute] = time.split(':').map(Number);
  let hour = parsedHour;

  // 24-hour input ('14:30') has no period; the 12-hour branches are kept for legacy values.
  // Only one letter here since '10 p. m.' will be split as ['10', 'p.', 'm.'] and not ['10', 'p. m.']
  if (period.toLowerCase() === 'p.' && hour < 12) {
    hour += 12; // Convert PM to 24-hour format
  } else if (period.toLowerCase() === 'a.' && hour === 12) {
    hour = 0; // Convert 12 AM to 0 hours
  }

  const date = new Date();
  date.setFullYear(new Date(dateStr).getFullYear());
  date.setMonth(new Date(dateStr).getMonth());
  date.setDate(new Date(dateStr).getDate());
  date.setHours(hour, minute, 0, 0);

  console.log('Reverted time:', date.toISOString(), date);
  return date.toISOString();
}

export function revertFormatDate(dateStr: string): string {
  const date = new Date(dateStr);

  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  const hours = `${date.getHours()}`.padStart(2, '0');
  const minutes = `${date.getMinutes()}`.padStart(2, '0');

  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

export function generateDateOptions(): string[] {
  const today = new Date();
  const dateOptions: string[] = [];
  if (today.getHours() >= 21 && today.getMinutes() > 15) {
    today.setDate(today.getDate() + 1); // If it's past 10 PM, start from tomorrow
  }

  const formatter = new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });

  for (let i = 0; i < 14; i++) {
    const date = new Date(today);
    date.setDate(today.getDate() + i);
    dateOptions.push(formatter.format(date));
  }

  return dateOptions;
}

export function generateTimeOptions(date: string): string[] {
  const dateObj = new Date(revertFormatDate(date));
  const now = new Date();
  const isToday = dateObj.toDateString() === now.toDateString();
  if (!isToday) {
    return generateTimeOptionsForDay('10:00', '22:00');
  }
  let hours = now.getHours();
  let minutes = now.getMinutes();
  const moreThan45Minutes = minutes >= 45;
  const moreThan15Minutes = minutes >= 15 && minutes < 45;
  const moreThan0Minutes = minutes >= 0 && minutes < 15;

  // Since reservations can only be made 15 minutes in advance,
  // we adjust the current time to the next half hour if it's less than 15 minutes
  if (moreThan45Minutes) {
    minutes = 30;
    hours += 1;
  } else if (moreThan15Minutes) {
    minutes = 0;
    hours += 1;
  } else if (moreThan0Minutes) {
    minutes = 30;
  }

  if (hours >= 22) {
    return [];
  }

  const startHours = new Date(1, 1, 1, hours, minutes, 0, 0);
  const endHours = new Date(1, 1, 1, 22, 0, 0, 0);
  const formattedStart = formatTime(startHours.toISOString());
  const formattedEnd = formatTime(endHours.toISOString());
  return generateTimeOptionsForDay(formattedStart, formattedEnd);
}

function generateTimeOptionsForDay(startTime: string, endTime: string): string[] {
  const start = new Date(revertFormatTime(startTime, new Date().toISOString()));
  const end = new Date(revertFormatTime(endTime, new Date().toISOString()));
  const timeOptions: string[] = [];

  while (start < end) {
    timeOptions.push(formatTime(start.toISOString()));
    start.setMinutes(start.getMinutes() + 30);
  }

  return timeOptions;
}

export function combineDateAndTime(dateStr: string, timeStr: string): string {
  const date = new Date(dateStr);
  const time = new Date(timeStr);

  const combinedDate = new Date(
    Date.UTC(
      date.getFullYear(),
      date.getMonth(),
      date.getDate(),
      time.getUTCHours(),
      time.getUTCMinutes()
    )
  );

  return combinedDate.toISOString();
}

const MILLISECONDS_PER_MINUTE = 60_000;

export function addMinutesIso(isoStr: string, minutes: number): string {
  return new Date(Date.parse(isoStr) + minutes * MILLISECONDS_PER_MINUTE).toISOString();
}

export function minutesBetween(startIso: string, endIso: string): number {
  return Math.round((Date.parse(endIso) - Date.parse(startIso)) / MILLISECONDS_PER_MINUTE);
}

export function findClosestDuration(minutes: number, durations: readonly number[]): number {
  return durations.reduce((closest, duration) =>
    Math.abs(duration - minutes) < Math.abs(closest - minutes) ? duration : closest
  );
}

export function parseToISO(dateStr: string, timeStr: string): string {
  const [hoursMinutes, period = ''] = timeStr.trim().split(' ');
  const [hoursStr, minutesStr] = hoursMinutes.split(':');

  let hours = Number(hoursStr);
  const minutes = Number(minutesStr);

  const lowerPeriod = period.toLowerCase();
  if (lowerPeriod.includes('p') && hours !== 12) hours += 12;
  if (lowerPeriod.includes('a') && hours === 12) hours = 0;

  const date = new Date(dateStr);
  date.setHours(hours, minutes, 0, 0);
  return date.toISOString();
}

export function addAndRoundBookingTime(
  timeStr: string,
  bookingMinutes = 15,
  roundStep = 15
): string {
  const date = new Date(timeStr);
  const totalMinutes = date.getMinutes() + bookingMinutes;
  date.setMinutes(totalMinutes);

  const roundedMinutes = Math.ceil(date.getMinutes() / roundStep) * roundStep;
  date.setMinutes(roundedMinutes);

  return date.toISOString();
}
