import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http'
import { environment } from 'src/environments/environment';
import { first } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class LpsolverService {

  constructor(private http: HttpClient) { }

  solveLP(lpModelQuery: any){
    return this.http.post<any>(`${environment.apiUrl}/solve`, lpModelQuery).pipe(first());
  }

  getResult(taskId: any){
    return this.http.post<any>(`${environment.apiUrl}/taskresult?taskId=${taskId}`, {}).pipe(first());
  }
}
