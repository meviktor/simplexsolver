import { Injectable } from '@angular/core';

@Injectable()
export class Utils{
    sequence(length: number, startFrom: number): number[] {
        return [...Array(length).keys()].map(i => i + startFrom);
      }
}